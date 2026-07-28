using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Reports.Queries.GetSalaryByType;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetSalaryByTypeQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly GetSalaryByTypeQueryHandler _handler;

    public GetSalaryByTypeQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();

        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);

        _handler = new GetSalaryByTypeQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSalaryNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetSalaryByTypeQuery(employeeId, SalaryType.Monthly);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync((Salary?)null);

        // ACT
        var act = async () => await _handler.Handle(query, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Employee Salary*{employeeId}*");
    }

    [Fact]
    public async Task Handle_WhenActiveSalaryExists_ShouldReturnAmountConvertedToRequestedType()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetSalaryByTypeQuery(employeeId, SalaryType.Weekly);

        // 100/hour * 2080 hours = 208 000 a year, i.e. 4 000 a week.
        // Conversion coverage itself lives in SalaryTest — here it only proves the
        // handler converts the *current* salary into the *requested* type.
        var salary = TestData.ActiveSalary(employeeId);
        salary.Amount = 100m;
        salary.Type = SalaryType.Hourly;

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync(salary);

        // ACT
        var result = await _handler.Handle(query, Ct);

        // ASSERT
        result.Should().Be(4_000m);

        _salaryRepositoryMock.Verify(r => r.GetCurrentSalaryAsync(employeeId, Ct), Times.Once);
    }
}
