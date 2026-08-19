using AccountingHelper.Application.Features.Reports.Queries.GetDashboard;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetDashboardQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly GetDashboardQueryHandler _handler;

    public GetDashboardQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();

        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);

        _handler = new GetDashboardQueryHandler(_unitOfWorkMock.Object);
    }

    private void SetupRepositories(
        IReadOnlyList<ActiveSalarySnapshot> snapshots,
        IReadOnlyList<DepartmentHeadcount> headcounts)
    {
        _salaryRepositoryMock
            .Setup(r => r.GetActiveSnapshotsAsync(Ct))
            .ReturnsAsync(snapshots);

        _employeeRepositoryMock
            .Setup(r => r.GetHeadcountByDepartmentAsync(Ct))
            .ReturnsAsync(headcounts);
    }

    // Пустая БД — единственный случай, где разница между Sum() и Average() видна:
    // Sum() по пустой последовательности возвращает 0, Average() бросил бы
    // InvalidOperationException. Здесь же проверяется, что токен уходит в оба репозитория.
    [Fact]
    public async Task Handle_WhenNothingToReport_ShouldReturnZerosAndEmptyLists()
    {
        // ARRANGE
        SetupRepositories([], []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.TotalMonthlyPayroll.Should().Be(0m);
        result.TotalMonthlyTax.Should().Be(0m);
        result.HeadcountByDepartment.Should().BeEmpty();
        result.AverageSalaryByGrade.Should().BeEmpty();

        _salaryRepositoryMock.Verify(r => r.GetActiveSnapshotsAsync(Ct), Times.Once);
        _employeeRepositoryMock.Verify(r => r.GetHeadcountByDepartmentAsync(Ct), Times.Once);
    }

    // Суммы посчитаны руками:
    //   Monthly 1000                     => 1000.00
    //   Hourly  10  => 10 * 2080 / 12    => 1733.33(3)
    //   Weekly  500 => 500 * 52 / 12     => 2166.66(6)
    // ФОТ = 1000 + 1733.33(3) + 2166.66(6) = 4900.00
    // Налог: все три ниже нижней границы шкалы, значит 10% с каждого => 490.00
    [Fact]
    public async Task Handle_ShouldConvertEverySalaryToMonthly_AndTaxEachEmployeeSeparately()
    {
        // ARRANGE
        SetupRepositories(
            [
                new ActiveSalarySnapshot(1000m, SalaryType.Monthly, EmployeeGrade.Junior),
                new ActiveSalarySnapshot(10m, SalaryType.Hourly, EmployeeGrade.Middle),
                new ActiveSalarySnapshot(500m, SalaryType.Weekly, EmployeeGrade.Senior)
            ],
            []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.TotalMonthlyPayroll.Should().Be(4900.00m);
        result.TotalMonthlyTax.Should().Be(490.00m);

        // Каждое поле округляется от своего точного значения: сумма средних (4900.00)
        // и округлённый ФОТ здесь совпадают случайно, а расходиться им не запрещено.
        result.AverageSalaryByGrade.Select(g => g.AverageMonthlySalary)
            .Should().Equal(1000.00m, 1733.33m, 2166.67m);
    }

    // Прогрессивная шкала: 500 000 попадает в 10%, 700 000 — в 20%.
    // Налог, взятый от общего ФОТ (1 200 000 => 30% => 360 000), дал бы другое число,
    // поэтому тест фиксирует именно поштучный расчёт: 50 000 + 140 000 = 190 000.
    [Fact]
    public async Task Handle_ShouldTaxPerEmployee_NotOnTotalPayroll()
    {
        // ARRANGE
        SetupRepositories(
            [
                new ActiveSalarySnapshot(500_000m, SalaryType.Monthly, EmployeeGrade.Lead),
                new ActiveSalarySnapshot(700_000m, SalaryType.Monthly, EmployeeGrade.Principal)
            ],
            []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.TotalMonthlyPayroll.Should().Be(1_200_000.00m);
        result.TotalMonthlyTax.Should().Be(190_000.00m);
    }

    // Среднее двух зарплат — ровно 1000.005, то есть середина.
    // AwayFromZero даёт 1000.01; дефолтное банковское округление дало бы 1000.00.
    [Fact]
    public async Task Handle_ShouldRoundHalfAwayFromZero()
    {
        // ARRANGE
        SetupRepositories(
            [
                new ActiveSalarySnapshot(1000.00m, SalaryType.Monthly, EmployeeGrade.Middle),
                new ActiveSalarySnapshot(1000.01m, SalaryType.Monthly, EmployeeGrade.Middle)
            ],
            []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.AverageSalaryByGrade.Should().ContainSingle()
            .Which.AverageMonthlySalary.Should().Be(1000.01m);
    }

    // Порядок грейдов — по значению enum (Junior -> Principal), а не по алфавиту:
    // алфавитный дал бы Junior, Senior наоборот и на большем наборе разъехался бы совсем.
    [Fact]
    public async Task Handle_ShouldGroupAveragesByGrade_OrderedByGradeValue()
    {
        // ARRANGE
        SetupRepositories(
            [
                new ActiveSalarySnapshot(5000m, SalaryType.Monthly, EmployeeGrade.Senior),
                new ActiveSalarySnapshot(5400m, SalaryType.Monthly, EmployeeGrade.Senior),
                new ActiveSalarySnapshot(1000m, SalaryType.Monthly, EmployeeGrade.Junior)
            ],
            []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.AverageSalaryByGrade.Select(g => g.Grade)
            .Should().Equal(EmployeeGrade.Junior, EmployeeGrade.Senior);

        result.AverageSalaryByGrade.Select(g => g.AverageMonthlySalary)
            .Should().Equal(1000.00m, 5200.00m);
    }

    // Грейды без людей в ответе не появляются: в наборе только Junior,
    // а остальные четыре значения enum молча отсутствуют.
    [Fact]
    public async Task Handle_ShouldSkipGradesWithoutEmployees()
    {
        // ARRANGE
        SetupRepositories(
            [new ActiveSalarySnapshot(1000m, SalaryType.Monthly, EmployeeGrade.Junior)],
            []);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.AverageSalaryByGrade.Should().ContainSingle()
            .Which.Grade.Should().Be(EmployeeGrade.Junior);
    }

    [Fact]
    public async Task Handle_ShouldReturnHeadcountOrderedByDepartmentName()
    {
        // ARRANGE
        var sales = new DepartmentHeadcount(Guid.NewGuid(), "Sales", 2);
        var it = new DepartmentHeadcount(Guid.NewGuid(), "IT", 3);
        var finance = new DepartmentHeadcount(Guid.NewGuid(), "Finance", 1);

        SetupRepositories([], [sales, it, finance]);

        // ACT
        var result = await _handler.Handle(new GetDashboardQuery(), Ct);

        // ASSERT
        result.HeadcountByDepartment.Select(h => h.DepartmentName)
            .Should().Equal("Finance", "IT", "Sales");

        // Id и количество едут вместе с именем, а не сортируются по отдельности.
        result.HeadcountByDepartment.Select(h => h.DepartmentId)
            .Should().Equal(finance.DepartmentId, it.DepartmentId, sales.DepartmentId);

        result.HeadcountByDepartment.Select(h => h.Headcount)
            .Should().Equal(1, 3, 2);
    }
}
