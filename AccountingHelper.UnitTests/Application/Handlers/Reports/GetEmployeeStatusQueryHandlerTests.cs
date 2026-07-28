using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Reports.Queries.GetEmployeeStatus;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetEmployeeStatusQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly GetEmployeeStatusQueryHandler _handler;

    public GetEmployeeStatusQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);

        _handler = new GetEmployeeStatusQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetEmployeeStatusQuery(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetStatusAsync(employeeId, Ct))
            .ReturnsAsync((EmployeeStatus?)null);

        // ACT
        var act = async () => await _handler.Handle(query, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Employee*{employeeId}*was not found*");
    }

    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.OnVacation)]
    [InlineData(EmployeeStatus.Fired)]
    public async Task Handle_WhenEmployeeFound_ShouldReturnEmployeeStatus(EmployeeStatus status)
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var query = new GetEmployeeStatusQuery(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetStatusAsync(employeeId, Ct))
            .ReturnsAsync(status);

        // ACT
        var result = await _handler.Handle(query, Ct);

        // ASSERT
        result.Should().Be(status);

        _employeeRepositoryMock.Verify(r => r.GetStatusAsync(employeeId, Ct), Times.Once);
    }
}
