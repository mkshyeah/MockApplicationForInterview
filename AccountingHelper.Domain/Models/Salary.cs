using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Services;

namespace AccountingHelper.Domain.Models;

public class Salary
{
    public Guid Id { get; set; }
    public required decimal Amount { get; set; }
    public required SalaryType Type { get; set; }
    public required DateTime EffectiveDate { get; set; } 
    public DateTime? EndDate { get; set; }
    public required Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    
    public decimal ConvertTo(SalaryType targetType) => SalaryCalculator.Convert(Amount, Type, targetType);
    public decimal CalculateTaxes() => SalaryCalculator.CalculateTaxes(Amount, Type);
}