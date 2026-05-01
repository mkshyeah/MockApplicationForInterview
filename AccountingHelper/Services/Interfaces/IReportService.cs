using AccountingHelper.Domain.Enums;
using ErrorOr;

namespace AccountingHelper.Services.Interfaces;

public interface IReportService
{
    Task<ErrorOr<int>> CountEmployees(CancellationToken ct);
    Task<ErrorOr<EmployeeStatus>> GetEmployeeStatus(Guid id, CancellationToken ct);

    Task<ErrorOr<decimal>> GetTotalSalaries(CancellationToken ct);

    Task<ErrorOr<decimal>> GetSalaryByType(Guid id, SalaryType type, CancellationToken ct);

    Task<ErrorOr<decimal>> CalculateTaxes(Guid id, CancellationToken ct);
}