using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.FireEmployee;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class FireEmployeeCommandHandlerTests
{
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

    private static Employee CreateEmployee(Guid id, EmployeeStatus status) => new()
    {
        Id = id,
        FirstName = "John",
        LastName = "Doe",
        Email = "J@mail.com",
        PositionId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        Status = status
    };

    [Fact]
    public async Task Handle_WhenEmployeeIsActive_ShouldSetFiredStatusAndCloseSalary()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var salaryId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

        var activeEmployee = CreateEmployee(employeeId, EmployeeStatus.Active);
        var activeSalary = new Salary
        {
            Id = salaryId,
            Amount = 100,
            Type = SalaryType.Monthly,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId
        };

        // A distinct token so the Verify calls below prove the handler forwards it
        // down every awaited call instead of silently passing CancellationToken.None.
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, ct))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, ct))
            .ReturnsAsync(activeSalary);

        _salaryRepositoryMock
            .Setup(r => r.CloseAsync(salaryId, ct))
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;

        // ACT
        var result = await _handler.Handle(command, ct);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.Fired);
        result.TerminationDate.Should().NotBeNull();
        result.TerminationDate!.Value.Should()
            .BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);

        _employeeRepositoryMock.Verify(r => r.GetByIdAsync(employeeId, ct), Times.Once);

        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, ct), Times.Once);

        _salaryRepositoryMock.Verify(r => r.CloseAsync(salaryId, ct), Times.Once);

        _employeeRepositoryMock.Verify(r => r.Update(activeEmployee), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(ct), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSalary_ShouldFireWithoutClosingSalary()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);
        var activeEmployee = CreateEmployee(employeeId, EmployeeStatus.Active);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Salary?)null);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Status.Should().Be(EmployeeStatus.Fired);

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyFired_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = new FireEmployeeCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmployee(employeeId, EmployeeStatus.Fired));

        // ACT
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>();

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var nonExistentId = Guid.NewGuid();
        var command = new FireEmployeeCommand(nonExistentId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // ACT
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>();

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}