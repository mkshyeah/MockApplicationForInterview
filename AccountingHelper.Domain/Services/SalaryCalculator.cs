using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Services;

public static class SalaryCalculator
{
    private const int HoursInYear = 2080;
    private const int MonthsInYear = 12;
    private const int WeeksInYear = 52;
    private const int DaysInYear = 365;

    private const decimal LowTaxBracket = 600_000m;
    private const decimal HighTaxBracket = 800_000m;
    private const decimal LowTaxRate = 0.10m;
    private const decimal MidTaxRate = 0.20m;
    private const decimal HighTaxRate = 0.30m;

    public static decimal Convert(decimal amount, SalaryType from, SalaryType to)
    {
        var annual = from switch
        {
            SalaryType.Hourly  => amount * HoursInYear,
            SalaryType.Daily   => amount * DaysInYear,
            SalaryType.Weekly  => amount * WeeksInYear,
            SalaryType.Monthly => amount * MonthsInYear,
            _ => throw new InvalidOperationException($"Unsupported current salary type: {from}")
        };

        return to switch
        {
            SalaryType.Hourly  => annual / HoursInYear,
            SalaryType.Daily   => annual / DaysInYear,
            SalaryType.Weekly  => annual / WeeksInYear,
            SalaryType.Monthly => annual / MonthsInYear,
            _ => throw new ArgumentOutOfRangeException(nameof(to),
                $"Requested salary type is invalid: {to}")
        };
    }

    public static decimal CalculateTaxes(decimal amount, SalaryType type)
    {
        var monthly = Convert(amount, type, SalaryType.Monthly);

        return monthly switch
        {
            <= LowTaxBracket  => monthly * LowTaxRate,
            <= HighTaxBracket => monthly * MidTaxRate,
            _                 => monthly * HighTaxRate
        };
    }
}