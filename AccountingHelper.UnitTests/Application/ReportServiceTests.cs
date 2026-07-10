using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application;

public class ReportServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();
        
        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);
        
        _reportService = new ReportService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CalculateTaxes_WhenSalaryNotFound_ShouldThrowNotFoundException()
    {
        //Arrange
        var employeeId = Guid.NewGuid();
        _salaryRepositoryMock
            .Setup(r => r.GetCurrentSalaryAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Salary) null!);
        
        // Act
        Func<Task> act = async () => await _reportService.CalculateTaxes(employeeId, CancellationToken.None);
        
        //Assert 
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Employee Salary*{employeeId}*");
    }
}