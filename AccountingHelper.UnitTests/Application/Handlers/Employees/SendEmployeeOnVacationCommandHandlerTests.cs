using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOnVacation;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Employees;

public class SendEmployeeOnVacationCommandHandlerTests
{
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly SendEmployeeOnVacationCommandHandler _handler;

    public SendEmployeeOnVacationCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendEmployeeOnVacationCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOnVacationCommand(employeeId);

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
        var command = new SendEmployeeOnVacationCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId, EmployeeStatus.Fired));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*fired employee on vacation*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyOnVacation_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOnVacationCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId, EmployeeStatus.OnVacation));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already on vacation*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployeeIsActive_ShouldSetOnVacationStatus()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new SendEmployeeOnVacationCommand(employeeId);

        var activeEmployee = TestData.ValidEmployee(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(activeEmployee);

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.OnVacation);

        _employeeRepositoryMock.Verify(r => r.GetByIdAsync(employeeId, Ct), Times.Once);
        _employeeRepositoryMock.Verify(r => r.Update(activeEmployee), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
