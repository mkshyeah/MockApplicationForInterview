using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.SendEmployeeOffVacation;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers;

public class SendEmployeeOffVacationCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly SendEmployeeOffVacationCommandHandler _handler;

    public SendEmployeeOffVacationCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendEmployeeOffVacationCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOffVacationCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync((Employee?)null);

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFired_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOffVacationCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId, EmployeeStatus.Fired));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*fired employee*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotOnVacation_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOffVacationCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*not currently on vacation*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployeeIsOnVacation_ShouldSetActiveStatus()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOffVacationCommand(employeeId);

        var employeeOnVacation = TestData.ValidEmployee(employeeId, EmployeeStatus.OnVacation);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(employeeOnVacation);

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.Active);

        _employeeRepositoryMock.Verify(r => r.GetByIdAsync(employeeId, Ct), Times.Once);
        _employeeRepositoryMock.Verify(r => r.Update(employeeOnVacation), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
