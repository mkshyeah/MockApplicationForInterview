using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using FluentAssertions;

namespace AccountingHelper.UnitTests.Domain.Models;

public class SalaryTest
{
    [Theory]
    // Тест-кейсы для Monthly (прямой расчет по брекетам)
    [InlineData(500000, SalaryType.Monthly, 50000)]  // Низкий брекет (<=600к): 500 000 * 10% = 50 000
    [InlineData(700000, SalaryType.Monthly, 140000)] // Средний брекет (<=800к): 700 000 * 20% = 140 000
    [InlineData(900000, SalaryType.Monthly, 270000)] // Высокий брекет (>800к): 900 000 * 30% = 270 000
    
    // Тест-кейсы для Hourly
    // 3000 * 2080 / 12 = 520 000 (Низкий брекет: 520 000 * 10% = 52 000)
    [InlineData(3000, SalaryType.Hourly, 52000)] 
    
    // Тест-кейсы для Daily
    // 24000 * 365 / 12 = 730 000 (Средний брекет: 730 000 * 20% = 146 000)
    [InlineData(24000, SalaryType.Daily, 146000)]

    // Тест-кейс для Weekly
    // 192000 * 52 / 12 = 832 000 (Высокий брекет: 832 000 * 30% = 249 600)
    [InlineData(192000, SalaryType.Weekly, 249600)]
    public void CalculateTaxes_ShouldApplyCorrectBracketRate_BasedOnSalaryType(
        decimal amount, 
        SalaryType type, 
        decimal expectedTax)
    {
        // 1. Arrange
        var salary = new Salary 
        { 
            Amount = amount, 
            EmployeeId = Guid.NewGuid(),
            Type = type ,
            EffectiveDate = DateTime.UtcNow
        };

        // 2. Act
        var actualTax = salary.CalculateTaxes();

        // 3. Assert
        actualTax.Should().Be(expectedTax);
    }
    
    [Theory]
    // Текущая зарплата, Текущий тип -> Целевой тип -> Ожидаемый результат
    [InlineData(1000, SalaryType.Monthly, SalaryType.Monthly, 1000)] // Из Monthly в Monthly
    [InlineData(100, SalaryType.Hourly, SalaryType.Monthly, 17333.3333)] // Из Hourly в Monthly (100 * 2080 / 12)
    [InlineData(12000, SalaryType.Daily, SalaryType.Weekly, 84230.7692)] // Из Daily в Weekly (12000 * 365 / 52)
    [InlineData(1000, SalaryType.Weekly, SalaryType.Hourly, 25)] // Из Weekly в Hourly (1000 * 52 / 2080)
    [InlineData(1000, SalaryType.Weekly, SalaryType.Daily, 142.4658)] // Из Weekly в Daily (1000 * 52 / 365)
    public void ConvertTo_ShouldCalculateCorrectAmount_BasedOnTargetType(
        decimal currentAmount, 
        SalaryType currentType, 
        SalaryType targetType, 
        decimal expectedAmount)
    {
        // Arrange
        var salary = new Salary { Amount = currentAmount, EmployeeId = Guid.NewGuid(),Type = currentType, EffectiveDate = DateTime.UtcNow };

        // Act
        var actualAmount = salary.ConvertTo(targetType);

        // Assert
        actualAmount.Should().BeApproximately(expectedAmount, 0.0001m);
    }
}