using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application;

public class ReportServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        
        _reportService = new ReportService(_unitOfWorkMock.Object);
    }

    #region GetEmployeeStatus
    [Fact]
    public async Task GetEmployeeStatus_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        
        _employeeRepositoryMock
            .Setup(r => r.GetStatusAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeStatus?) null);
        
        //ACT
        var act = async () => await _reportService.GetEmployeeStatus(employeeId, CancellationToken.None);
        
        //ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
        
    }

    [Fact]
    public async Task GetEmployeeStatus_WhenEmployeeFound_ShouldReturnEmployeeStatus()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        
        _employeeRepositoryMock
            .Setup(r => r.GetStatusAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmployeeStatus.Active);
        
        //ACT
        var result = await _reportService.GetEmployeeStatus(employeeId, CancellationToken.None);
        
        //ASSERT
        result.Should().Be(EmployeeStatus.Active);
    }

    #endregion
    

    #region GetSalaryByType

    [Fact]
    public async Task GetSalaryByType_WhenSalaryNotFound_ShouldThrowNotFoundException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var salaryType = SalaryType.Monthly;
        
        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Salary?) null);
        
        //ACT
        var act = async() => await _reportService.GetSalaryByType(employeeId, salaryType, CancellationToken.None);
        
        //ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
    }
    #endregion


    #region CalculateTaxes

    [Fact]
    public async Task CalculateTaxes_WhenSalaryNotFound_ShouldThrowNotFoundException()
    {
        //Arrange
        var employeeId = Guid.NewGuid();
        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Salary) null!);
        
        // Act
        var act = async () => await _reportService.CalculateTaxes(employeeId, CancellationToken.None);
        
        //Assert 
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Employee Salary*{employeeId}*");
    }
    
    [Fact]
    public async Task CalculateTaxes_WhenSalaryFound_ShouldReturnCorrectTax()
    {
        var employeeId = Guid.NewGuid();

        var salary = new Salary
        {
            Amount = 700000m,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employeeId,
            Type = SalaryType.Monthly
        };
        
        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salary);
        
        //ACT
        var result = await _reportService.CalculateTaxes(employeeId, CancellationToken.None);
        
        //ASSERT
        result.Should().Be(140000);
    }
    

    #endregion
    
}