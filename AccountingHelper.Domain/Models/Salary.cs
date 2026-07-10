using AccountingHelper.Domain.Enums;

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
    
    private const int HoursInYear = 2080;
    private const int MonthsInYear = 12;
    private const int WeeksInYear = 52;
    private const int DaysInYear = 365;
    
    private const decimal LowTaxBracket = 600_000m;
    private const decimal HighTaxBracket = 800_000m;
    private const decimal LowTaxRate = 0.10m;
    private const decimal MidTaxRate = 0.20m;
    private const decimal HighTaxRate = 0.30m;

    public decimal CalculateTaxes()
    {
        var monthlyAmount = Type switch
        {
            SalaryType.Monthly => Amount,
            SalaryType.Hourly => Amount * HoursInYear / MonthsInYear,
            SalaryType.Daily => Amount * DaysInYear / MonthsInYear,
            SalaryType.Weekly => Amount * WeeksInYear / MonthsInYear,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return monthlyAmount switch
        {
            <= LowTaxBracket  => monthlyAmount * LowTaxRate,
            <= HighTaxBracket => monthlyAmount * MidTaxRate,
            _                 => monthlyAmount * HighTaxRate
        };
    }

    public decimal ConvertTo(SalaryType targetType)
    {
        var annualSalary = Type switch
        {
            SalaryType.Hourly => Amount * HoursInYear,
            SalaryType.Daily => Amount * DaysInYear,
            SalaryType.Weekly => Amount * WeeksInYear,
            SalaryType.Monthly => Amount * MonthsInYear,
            _ => throw new InvalidOperationException($"Unsupported current salary type: {Type}")
        };

        return targetType switch
        {
            SalaryType.Hourly => annualSalary / HoursInYear,
            SalaryType.Daily => annualSalary / DaysInYear,
            SalaryType.Weekly => annualSalary / WeeksInYear,
            SalaryType.Monthly => annualSalary / MonthsInYear,
            _ => throw new ArgumentOutOfRangeException(nameof(targetType),
                $"Requested salary type is invalid: {targetType}")
        };
    }
}