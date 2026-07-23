using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application;

public class SalaryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<ILogger<SalaryService>> _loggerMock;
    private readonly SalaryService _salaryService;

    public SalaryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);

        _loggerMock = new Mock<ILogger<SalaryService>>();

        _salaryService = new SalaryService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region ChangeSalary Tests

    [Fact]
    public async Task ChangeSalary_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var newSalaryType = SalaryType.Monthly;
        var newSalaryAmount = 1000m;

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?) null);
        
        //ACT
        var act = async() => await _salaryService.ChangeSalary(employeeId, newSalaryType, newSalaryAmount, CancellationToken.None);
        
        //ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
        
        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()),
            Times.Never);
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _salaryRepositoryMock.Verify(
            r => r.Add(It.IsAny<Salary>()),
            Times.Never);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSalary_WhenEmployeeIsFired_ShouldThrowBusinessRuleException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var newSalaryType = SalaryType.Monthly;
        var newSalaryAmount = 1000m;

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
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firedEmployee);
        
        //ACT
        var act = async() => await _salaryService.ChangeSalary(employeeId, newSalaryType, newSalaryAmount, CancellationToken.None);
        
        //ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>();
        
        _salaryRepositoryMock.Verify(
            r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()),
            Times.Never);
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _salaryRepositoryMock.Verify(
            r => r.Add(It.IsAny<Salary>()),
            Times.Never);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSalary_WhenCurrentSalaryExists_ShouldCallCloseAsync()
    {
        var employeeId = Guid.NewGuid();
        var currentSalaryId = Guid.NewGuid();
        var newSalaryId = Guid.NewGuid();

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

        var newSalary = new Salary
        {
            Id = newSalaryId,
            Amount = 1000,
            Type = SalaryType.Monthly,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId
        };

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldSalary);
        
        _salaryRepositoryMock
            .Setup(r => r.CloseAsync(oldSalary.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //ACT
        var result =  await _salaryService.ChangeSalary(employeeId, newSalary.Type, newSalary.Amount, CancellationToken.None);
        
        //ASSERT
        result.Amount.Should().Be(newSalary.Amount);
        result.Type.Should().Be(newSalary.Type);
        result.EmployeeId.Should().Be(employeeId);
        result.Id.Should().NotBeEmpty();
        
        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(oldSalary.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == newSalary.Amount &&
            s.Type == newSalary.Type &&
            s.EmployeeId == employeeId)), Times.Once);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
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

        var newSalary = new Salary
        {
            Amount = 1000,
            Type = SalaryType.Monthly,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId
        };
        
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEmployee);

        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Salary?)null);
        
        
        //ACT
        var result = await _salaryService.ChangeSalary(employeeId, newSalary.Type, newSalary.Amount, CancellationToken.None);
        
        //ASSERT
        result.Amount.Should().Be(newSalary.Amount);
        result.Type.Should().Be(newSalary.Type);
        result.EmployeeId.Should().Be(employeeId);
        result.Id.Should().NotBeEmpty();

        _salaryRepositoryMock.Verify(
            r => r.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        
        _salaryRepositoryMock.Verify(r => r.Add(It.Is<Salary>(s =>
            s.Amount == newSalary.Amount &&
            s.Type == newSalary.Type &&
            s.EmployeeId == employeeId)), Times.Once);
        
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

#endregion
}