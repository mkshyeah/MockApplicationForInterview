using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Salaries.Commands.ChangeSalary;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Salaries;

public class ChangeSalaryCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<ILogger<ChangeSalaryCommandHandler>> _loggerMock;
    private readonly ChangeSalaryCommandHandler _handler;

    public ChangeSalaryCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _loggerMock = new Mock<ILogger<ChangeSalaryCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ChangeSalaryCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    private static ChangeSalaryCommand ValidCommand(Guid? employeeId = null) => new(
        EmployeeId: employeeId ?? Guid.NewGuid(),
        Amount: 1000,
        SalaryType: SalaryType.Monthly);

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync((Employee?)null);

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");

        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()), Times.Never);
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _salaryRepositoryMock.Verify(r => r.Add(It.IsAny<Salary>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployeeIsFired_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(TestData.ValidEmployee(employeeId, EmployeeStatus.Fired));

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot change salary of a fired employee*");

        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()), Times.Never);
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _salaryRepositoryMock.Verify(r => r.Add(It.IsAny<Salary>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurrentSalaryExists_ShouldCloseItAndOpenNewOne()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

        var activeEmployee = TestData.ValidEmployee(employeeId);
        var oldSalary = TestData.ActiveSalary(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync(oldSalary);

        _salaryRepositoryMock
            .Setup(r => r.CloseAsync(oldSalary.Id, Ct))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        result.Id.Should().NotBeEmpty().And.NotBe(oldSalary.Id);
        result.Amount.Should().Be(command.Amount);
        result.Type.Should().Be(command.SalaryType);
        result.EmployeeId.Should().Be(employeeId);
        result.EndDate.Should().BeNull();

        _salaryRepositoryMock.Verify(r => r.CloseAsync(oldSalary.Id, Ct), Times.Once);
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == command.Amount &&
            s.Type == command.SalaryType &&
            s.EmployeeId == employeeId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoCurrentSalary_ShouldOpenNewOneWithoutClosing()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

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
        result.Id.Should().NotBeEmpty();
        result.Amount.Should().Be(command.Amount);
        result.Type.Should().Be(command.SalaryType);
        result.EmployeeId.Should().Be(employeeId);
        result.EndDate.Should().BeNull();

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == command.Amount &&
            s.Type == command.SalaryType &&
            s.EmployeeId == employeeId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
