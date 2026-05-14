using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.Interfaces;

public interface IReportService
{
    Task<int> CountEmployees(CancellationToken ct);
    Task<EmployeeStatus> GetEmployeeStatus(Guid employeeId, CancellationToken ct);

    Task<decimal> GetTotalSalaries(CancellationToken ct);

    Task<decimal> GetSalaryByType(Guid employeeId, SalaryType type, CancellationToken ct);

    Task<decimal> CalculateTaxes(Guid employeeId, CancellationToken ct);
}