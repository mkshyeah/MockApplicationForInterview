using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class DashboardTests : IntegrationTestBase
{
    public DashboardTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Route = "v1/reporting/dashboard";

    private async Task<DashboardResponse> GetDashboardAsync()
    {
        var resp = await Client.GetAsync(Route);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<DashboardResponse>(Json);
        return body!;
    }

    // Полный проход по контракту на сидированных данных. Всё посчитано руками:
    //
    //   IT / Junior   Monthly 1000  => 1000
    //   IT / Senior   Monthly 5000  => 5000
    //   IT / Senior   Monthly 5400  => 5400
    //   Finance / Junior Weekly 600 => 600 * 52 / 12 = 2600
    //   Finance / Middle Monthly 3000 => 3000
    //
    //   ФОТ    = 17 000
    //   Налог  = 10% с каждого (все ниже нижней границы шкалы) = 1700
    //   Люди   = Finance 2, IT 3
    //   Средние: Junior (1000 + 2600) / 2 = 1800, Middle 3000, Senior (5000 + 5400) / 2 = 5200
    //
    // Заодно это единственное место, где проверяется, что GROUP BY по департаментам вообще
    // транслируется в SQL: на моках в юнит-тестах репозитория нет.
    [Fact]
    public async Task GetDashboard_ReturnsHandCalculatedTotalsAndBreakdowns()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var financeId = await SeedDepartmentAsync("Finance");

        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var middleId = await SeedPositionAsync("Middle Developer", EmployeeGrade.Middle);
        var seniorId = await SeedPositionAsync("Senior Developer", EmployeeGrade.Senior);

        await CreateEmployeeAsync(itId, juniorId, salary: 1000m);
        await CreateEmployeeAsync(itId, seniorId, salary: 5000m);
        await CreateEmployeeAsync(itId, seniorId, salary: 5400m);
        await CreateEmployeeAsync(financeId, juniorId, salary: 600m, salaryType: "Weekly");
        await CreateEmployeeAsync(financeId, middleId, salary: 3000m);

        // ACT
        var dashboard = await GetDashboardAsync();

        // ASSERT
        dashboard.TotalMonthlyPayroll.Should().Be(17_000.00m);
        dashboard.TotalMonthlyTax.Should().Be(1_700.00m);

        // порядок — по имени департамента, поэтому Finance раньше IT
        dashboard.HeadcountByDepartment.Select(h => (h.DepartmentId, h.DepartmentName, h.Headcount))
            .Should().Equal(
                (financeId, "Finance", 2),
                (itId, "IT", 3));

        // порядок — по значению грейда, а не по алфавиту
        dashboard.AverageSalaryByGrade.Select(g => (g.Grade, g.AverageMonthlySalary))
            .Should().Equal(
                (EmployeeGrade.Junior, 1_800.00m),
                (EmployeeGrade.Middle, 3_000.00m),
                (EmployeeGrade.Senior, 5_200.00m));
    }

    // Уволенный обязан выпасть из обоих агрегатов сразу. Департамент Finance здесь состоит
    // из одного человека, и после увольнения он исчезает из headcount целиком — категории
    // с нулём в ответе не появляются.
    [Fact]
    public async Task GetDashboard_ExcludesFiredEmployee_FromPayrollAndHeadcount()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var financeId = await SeedDepartmentAsync("Finance");
        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);

        await CreateEmployeeAsync(itId, juniorId, salary: 1000m);
        await CreateEmployeeAsync(itId, juniorId, salary: 2000m);
        var fired = await CreateEmployeeAsync(financeId, juniorId, salary: 3000m);

        var fireResp = await Client.PatchAsync($"v1/employees/{fired.Id}/fire", content: null);
        fireResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT
        var dashboard = await GetDashboardAsync();

        // ASSERT
        dashboard.TotalMonthlyPayroll.Should().Be(3_000.00m); // 1000 + 2000, без уволенного
        dashboard.TotalMonthlyTax.Should().Be(300.00m);

        // Finance в ответе не остаётся вовсе: категорий с нулём мы не отдаём
        var remaining = dashboard.HeadcountByDepartment.Should().ContainSingle().Subject;
        remaining.DepartmentId.Should().Be(itId);
        remaining.DepartmentName.Should().Be("IT");
        remaining.Headcount.Should().Be(2);

        dashboard.AverageSalaryByGrade.Should().ContainSingle()
            .Which.AverageMonthlySalary.Should().Be(1_500.00m);
    }

    // Смена зарплаты закрывает старую и открывает новую, ФОТ обязан показать новую.
    // Этот же сценарий станет базой для теста устаревания кэша в блоке 3: там второй
    // запрос после смены зарплаты должен, наоборот, вернуть старые 1000 из кэша.
    [Fact]
    public async Task GetDashboard_ReflectsSalaryChangeInPayroll()
    {
        // ARRANGE
        var itId = await SeedDepartmentAsync("IT");
        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);

        var employee = await CreateEmployeeAsync(itId, juniorId, salary: 1000m);

        var before = await GetDashboardAsync();
        before.TotalMonthlyPayroll.Should().Be(1_000.00m);

        var changeResp = await Client.PutAsJsonAsync($"v1/employees/{employee.Id}/salaries", new
        {
            amount = 10_000m, salaryType = "Monthly"
        });
        changeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT
        var dashboard = await GetDashboardAsync();

        // ASSERT
        // 10 000, а не 11 000: старая зарплата закрыта и в активный срез не попадает
        dashboard.TotalMonthlyPayroll.Should().Be(10_000.00m);
        dashboard.TotalMonthlyTax.Should().Be(1_000.00m);

        dashboard.AverageSalaryByGrade.Should().ContainSingle()
            .Which.AverageMonthlySalary.Should().Be(10_000.00m);
    }
}
