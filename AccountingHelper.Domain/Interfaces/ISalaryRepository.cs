using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface ISalaryRepository
{
    Task<Salary?> GetCurrentSalaryAsync(Guid employeeId, CancellationToken ct);
    
    Task<decimal> GetTotalCurrentSalaryAsync(CancellationToken ct);
    Task<IReadOnlyList<Salary>> GetHistoryAsync(Guid employeeId, CancellationToken ct);
    
    Task CloseAsync (Guid salaryId, CancellationToken ct);
    void Add(Salary model);
}