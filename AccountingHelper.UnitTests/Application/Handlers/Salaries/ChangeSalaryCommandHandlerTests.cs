using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Salaries.ChangeSalary;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Salaries;

public class ChangeSalaryCommandHandlerTests
{
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

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);

        _loggerMock = new Mock<ILogger<ChangeSalaryCommandHandler>>();

        _handler = new ChangeSalaryCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);

    }
    
    public static ChangeSalaryCommand ValidCommand(
        Guid? employeeId = null
        ) => new(
        EmployeeId:employeeId ?? Guid.NewGuid(), 
        Amount: 1000,
        SalaryType: SalaryType.Monthly);
    

    [Fact]
    public async Task ChangeSalary_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync((Employee?) null);
        
        // ACT
        var act = async() => await _handler.Handle(command, Ct);
        
        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
        
        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, Ct),
            Times.Never);
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), Ct),
            Times.Never);

        _salaryRepositoryMock.Verify(
            r => r.Add(It.IsAny<Salary>()),
            Times.Never);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(Ct),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSalary_WhenEmployeeIsFired_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);

        var firedEmployee = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "J@mail.com",
            DepartmentId = Guid.NewGuid(),
            PositionId = Guid.NewGuid(),
            Status = EmployeeStatus.Fired,
        };
        
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(firedEmployee);
        
        // ACT
        var act = async() => await _handler.Handle(command, Ct);
        
        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>();
        
        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, Ct),
            Times.Never);
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), Ct),
            Times.Never);

        _salaryRepositoryMock.Verify(
            r => r.Add(It.IsAny<Salary>()),
            Times.Never);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(Ct),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSalary_WhenCurrentSalaryExists_ShouldCallCloseAsync()
    {
        var employeeId = Guid.NewGuid();
        var currentSalaryId = Guid.NewGuid();

        var activeEmployee = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "J@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Active
        };

        var oldSalary = new Salary
        {
            Id = currentSalaryId,
            Amount = 100,
            Type = SalaryType.Monthly,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId,
        };

        var command = ValidCommand(employeeId);

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
        var result =  await _handler.Handle(command, Ct);
        
        // ASSERT
        result.Amount.Should().Be(command.Amount);
        result.Type.Should().Be(command.SalaryType);
        result.EmployeeId.Should().Be(employeeId);
        result.Id.Should().NotBeEmpty();
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(oldSalary.Id, Ct),
            Times.Once);
        
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == command.Amount &&
            s.Type == command.SalaryType &&
            s.EmployeeId == employeeId)), Times.Once);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(Ct),
            Times.Once);
    }

    [Fact]
    public async Task ChangeSalary_WhenNoCurrentSalary_ShouldNotCallCloseAsync()
    {
        var employeeId = Guid.NewGuid();

        var activeEmployee = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "J@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Active
        };

        var command = ValidCommand(employeeId);
        
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, Ct))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, Ct))
            .ReturnsAsync((Salary?)null);
        
        
        // ACT
        var result = await _handler.Handle(command, Ct);
        
        // ASSERT
        result.Amount.Should().Be(command.Amount);
        result.Type.Should().Be(command.SalaryType);
        result.EmployeeId.Should().Be(employeeId);
        result.Id.Should().NotBeEmpty();

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), Ct),
            Times.Never);
        
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == command.Amount &&
            s.Type == command.SalaryType &&
            s.EmployeeId == employeeId)), Times.Once);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(Ct),
            Times.Once);
    }
}