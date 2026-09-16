using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public sealed record PayrollEmployeeSnapshot(
    Guid EmployeeId,
    decimal? Amount,
    SalaryType? SalaryType
);