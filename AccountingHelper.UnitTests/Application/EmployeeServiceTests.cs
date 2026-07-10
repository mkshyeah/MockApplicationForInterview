using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application;

public class EmployeeServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly EmployeeService _employeeService;
    
    public EmployeeServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _loggerMock = new Mock<ILogger<EmployeeService>>();
        _employeeService = new EmployeeService();
    }
    
    #region FireEmployee Tests

    [Fact]
    public void FireEmployee_WhenEmployeeIsActive_ShouldSetFiredStatusAndCloseSalary()
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
        var activeSalary = new Salary
        {
            Amount = 100,
            Type = SalaryType.Monthly,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId,
        };
    }
    #endregion
    
}