using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IPayrollRunRepository
{
    Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsForPeriodAsync(int year, int month, CancellationToken ct);
    void Add(PayrollRun payrollRun);
    Task ApplyProgressAsync(PayrollRun model, CancellationToken ct);
}