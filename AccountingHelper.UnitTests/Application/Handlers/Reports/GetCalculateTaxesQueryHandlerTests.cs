using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Reports.Queries.GetCalculateTaxes;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetCalculateTaxesQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly GetCalculateTaxesQueryHandler _handler;

    public GetCalculateTaxesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();

        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);

        _handler = new GetCalculateTaxesQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSalaryNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetCalculateTaxesQuery(employeeId);

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
    public async Task Handle_WhenActiveSalaryExists_ShouldReturnTaxForThatSalary()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetCalculateTaxesQuery(employeeId);

        // 700 000 monthly falls into the mid bracket: 700 000 * 20% = 140 000.
        // Bracket coverage itself lives in SalaryTest — here it only proves the
        // handler returns the tax of the *current* salary.
        var salary = TestData.ActiveSalary(employeeId);
        salary.Amount = 700_000m;
        salary.Type = SalaryType.Monthly;

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync(salary);

        // ACT
        var result = await _handler.Handle(query, Ct);

        // ASSERT
        result.Should().Be(140_000m);

        _salaryRepositoryMock.Verify(r => r.GetCurrentSalaryAsync(employeeId, Ct), Times.Once);
    }
}
