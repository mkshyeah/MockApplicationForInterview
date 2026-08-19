using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public sealed record ActiveSalarySnapshot(decimal Amount, SalaryType Type, EmployeeGrade Grade);