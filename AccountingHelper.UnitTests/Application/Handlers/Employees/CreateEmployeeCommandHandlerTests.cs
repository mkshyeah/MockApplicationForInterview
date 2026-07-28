using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Employees;

public class CreateEmployeeCommandHandlerTests
{
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;
    private readonly CreateEmployeeCommandHandler _handler;

    public CreateEmployeeCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _positionRepositoryMock = new Mock<IPositionRepository>();
        _loggerMock = new Mock<ILogger<CreateEmployeeCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Positions).Returns(_positionRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateEmployeeCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    private static CreateEmployeeCommand ValidCommand(
        Guid? departmentId = null,
        Guid? positionId = null) => new(
        FirstName: "John",
        LastName: "Doe",
        Email: "john@mail.com",
        SalaryAmount: 1000,
        SalaryType: SalaryType.Monthly,
        PositionId: positionId ?? Guid.NewGuid(),
        DepartmentId: departmentId ?? Guid.NewGuid(),
        HireDate: new DateTime(2024, 1, 15));

    [Fact]
    public async Task Handle_WhenEmailExists_ShouldThrowConflictException()
    {
        // ARRANGE
        var command = ValidCommand();

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, Ct))
            .ReturnsAsync(true);

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");

        _employeeRepositoryMock.Verify(r => r.Add(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDepartmentNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var command = ValidCommand();

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, Ct))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DepartmentId, Ct))
            .ReturnsAsync((Department?)null);

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Department*");

        _positionRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _employeeRepositoryMock.Verify(r => r.Add(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPositionNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var command = ValidCommand();

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, Ct))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DepartmentId, Ct))
            .ReturnsAsync(TestData.ValidDepartment(command.DepartmentId));

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PositionId, Ct))
            .ReturnsAsync((Position?)null);

        // ACT
        var act = async () => await _handler.Handle(command, Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Position*");

        _employeeRepositoryMock.Verify(r => r.Add(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDataIsValid_ShouldCreateActiveEmployeeWithSalary()
    {
        // ARRANGE
        var command = ValidCommand();

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, Ct))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DepartmentId, Ct))
            .ReturnsAsync(TestData.ValidDepartment(command.DepartmentId));

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PositionId, Ct))
            .ReturnsAsync(TestData.ValidPosition(command.PositionId));

        Employee? added = null;
        _employeeRepositoryMock
            .Setup(r => r.Add(It.IsAny<Employee>()))
            .Callback<Employee>(e => added = e);

        // the handler re-reads the entity after saving
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct))
            .ReturnsAsync(() => added);

        // ACT
        var result = await _handler.Handle(command, Ct);

        // ASSERT
        added.Should().NotBeNull();
        added!.Id.Should().NotBeEmpty();
        added.Status.Should().Be(EmployeeStatus.Active);
        added.Email.Should().Be(command.Email);
        added.HireDate.Should().Be(command.HireDate);

        added.Salaries.Should().ContainSingle();
        var salary = added.Salaries.Single();
        salary.Id.Should().NotBeEmpty();
        salary.EmployeeId.Should().Be(added.Id);
        salary.Amount.Should().Be(command.SalaryAmount);
        salary.Type.Should().Be(command.SalaryType);

        result.Should().BeSameAs(added);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
