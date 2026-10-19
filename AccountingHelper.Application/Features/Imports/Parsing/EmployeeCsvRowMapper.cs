using System.Globalization;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.Features.Imports.Parsing;

public sealed record CsvRowMapping(CreateEmployeeCommand? Command, IReadOnlyList<string> Errors);

// Turns text into a command: conversions and reference lookups only. The business rules stay in
// CreateEmployeeCommandValidator, which the run handler applies to the result.
public static class EmployeeCsvRowMapper
{
    // No thousands separator on purpose: with it, "1000,50" from a ru-locale file would parse as 100050.
    private const NumberStyles AmountStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    // Part of the file contract (see CLAUDE.md). ISO for machines, dd.MM.yyyy because that is what Excel
    // writes in ru/kk locales. Day first only: MM/dd/yyyy is deliberately absent, or 01.02.2024 would be
    // ambiguous.
    private static readonly string[] HireDateFormats = ["yyyy-MM-dd", "dd.MM.yyyy"];

    public static CsvRowMapping Map(
        EmployeeCsvRow row,
        IReadOnlyDictionary<string, Guid> departments,
        IReadOnlyDictionary<string, Guid> positions)
    {
        var errors = new List<string>();

        if (!decimal.TryParse(row.SalaryAmount, AmountStyle, CultureInfo.InvariantCulture, out var amount))
            errors.Add($"SalaryAmount '{row.SalaryAmount}' is not a number; use '.' as the decimal separator.");

        var salaryType = ParseSalaryType(row.SalaryType);
        if (salaryType is null)
            errors.Add($"SalaryType '{row.SalaryType}' must be one of: {string.Join(", ", Enum.GetNames<SalaryType>())}.");

        // UTC because hire_date is timestamptz and Npgsql rejects an Unspecified kind.
        if (!DateTime.TryParseExact(row.HireDate, HireDateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var hireDate))
            errors.Add($"HireDate '{row.HireDate}' must be a date in {string.Join(" or ", HireDateFormats)} format.");

        if (!departments.TryGetValue(row.Department ?? "", out var departmentId))
            errors.Add($"Unknown department '{row.Department}'.");

        if (!positions.TryGetValue(row.Position ?? "", out var positionId))
            errors.Add($"Unknown position '{row.Position}'.");

        if (errors.Count > 0)
            return new CsvRowMapping(null, errors);

        var command = new CreateEmployeeCommand(
            row.FirstName ?? "", row.LastName ?? "", row.Email ?? "",
            amount, salaryType!.Value, positionId, departmentId, hireDate);

        return new CsvRowMapping(command, errors);
    }

    // Enum.TryParse would also accept "3"; only the names are a contract.
    private static SalaryType? ParseSalaryType(string? value)
    {
        var name = Enum.GetNames<SalaryType>()
            .FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));

        return name is null ? null : Enum.Parse<SalaryType>(name);
    }
}
