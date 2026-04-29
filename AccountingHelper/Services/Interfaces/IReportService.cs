using AccountingHelper.Models;
using AccountingHelper.Models.Enums;

namespace AccountingHelper.Services;

public interface IReportService
{
    Task<int> CountEmployees(CancellationToken ct);
    Task<EmployeeStatus> GetEmployeeStatus(Guid id, CancellationToken ct);

    Task<decimal> GetTotalSalaries(CancellationToken ct);

    Task<decimal> GetSalaryByType(Guid id, string type, CancellationToken ct);

    Task<decimal> CalculateTaxes(Guid id, CancellationToken ct);
}