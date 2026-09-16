using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IPayslipRepository
{
    void AddRange(IEnumerable<Payslip> payslips);
    Task<IReadOnlyList<Payslip>> GetByRunAsync(Guid payrollRunId, CancellationToken ct);
}