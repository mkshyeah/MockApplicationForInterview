using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.FireEmployee;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers;

public class FireEmployeeCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<ILogger<FireEmployeeCommandHandler>> _loggerMock;
    private readonly FireEmployeeCommandHandler _handler;

    public FireEmployeeCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _loggerMock = new Mock<ILogger<FireEmployeeCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new FireEmployeeCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

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
    public async Task Handle_WhenAlreadyFired_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId, EmployeeStatus.Fired));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already fired*");

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployeeIsActive_ShouldSetFiredStatusAndCloseSalary()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var salaryId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

        var activeEmployee = TestData.ValidEmployee(employeeId);
        var activeSalary = TestData.ActiveSalary(employeeId, salaryId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync(activeSalary);

        _salaryRepositoryMock
            .Setup(r => r.CloseAsync(salaryId, Ct))
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.Fired);
        result.TerminationDate.Should().NotBeNull();
        result.TerminationDate!.Value.Should()
            .BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);

        _employeeRepositoryMock.Verify(r => r.GetByIdAsync(employeeId, Ct), Times.Once);
        _salaryRepositoryMock.Verify(r => r.GetCurrentSalaryAsync(employeeId, Ct), Times.Once);
        _salaryRepositoryMock.Verify(r => r.CloseAsync(salaryId, Ct), Times.Once);
        _employeeRepositoryMock.Verify(r => r.Update(activeEmployee), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSalary_ShouldFireWithoutClosingSalary()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

        var activeEmployee = TestData.ValidEmployee(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync((Salary?)null);

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.Fired);

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _employeeRepositoryMock.Verify(r => r.Update(activeEmployee), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
