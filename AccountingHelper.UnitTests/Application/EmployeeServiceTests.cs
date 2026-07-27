using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application;

public class EmployeeServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _positionRepositoryMock = new Mock<IPositionRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Positions).Returns(_positionRepositoryMock.Object);

        _loggerMock = new Mock<ILogger<EmployeeService>>();

        _employeeService = new EmployeeService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region SendOnVacationEmployee Tests

    [Fact]
    public async Task SendOnVacation_WhenFired_ShouldThrowBusinessRuleException()
    {
        var employeeId = Guid.NewGuid();
        
        var firedEmployee = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "j@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Fired
        };
        
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firedEmployee);
        
        //ACT
        var act = async() => await _employeeService.SendOnVacation(employeeId, CancellationToken.None);
        
        //ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task SendOnVacation_WhenAlreadyOnVacation_ShouldThrowBusinessRuleException()
    {
        var employeeId = Guid.NewGuid();
        
        var employeeOnVacation = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "j@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.OnVacation
        };
        
        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeOnVacation);
        
        //ACT

        var act = async () => await _employeeService.SendOnVacation(employeeId, CancellationToken.None);
        
        //ASSERT
        
        await act.Should().ThrowAsync<BusinessRuleException>();
    }
    
    [Fact]
    public async Task SendOffVacation_WhenNotOnVacation_ShouldThrowBusinessRuleException()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();

        var activeEmployee = new Employee
        {
            Id = employeeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "j@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Active 
        };

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEmployee);

        // ACT
        var act = async () => await _employeeService.SendOffVacation(employeeId, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>();
    }
    #endregion
    
    #region CreateEmployee Tests

    [Fact]
    public async Task CreateEmployee_WhenEmailExists_ShouldThrowConflictException()
    {
        // ARRANGE
        var newEmployee = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Active
        };
        
        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(newEmployee.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // ACT
        var act = async () => await _employeeService.CreateEmployee(newEmployee, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ConflictException>();
         
        _departmentRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEmployee_WhenDepartmentNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var departmentId = Guid.NewGuid();

        var newEmployee = new Employee
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@mail.com",
            PositionId = Guid.NewGuid(),
            DepartmentId = departmentId,
            Status = EmployeeStatus.Active
        };
        
        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(newEmployee.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _departmentRepositoryMock
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // ACT
        var act = async () => await _employeeService.CreateEmployee(newEmployee, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
        
        _positionRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEmployee_WhenPositionNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var positionId = Guid.NewGuid();

        var newEmployee = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "John@mail.com",
            PositionId = positionId,
            DepartmentId = Guid.NewGuid(),
            Status = EmployeeStatus.Active
        };

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(newEmployee.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Department найден — проходим второй барьер, падаем на третьем
        _departmentRepositoryMock
            .Setup(d => d.GetByIdAsync(newEmployee.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Name = "HR" });

        // Position не найден — здесь должен упасть
        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Position?)null);

        // ACT
        var act = async () => await _employeeService.CreateEmployee(newEmployee, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
    }
    
    #endregion
}