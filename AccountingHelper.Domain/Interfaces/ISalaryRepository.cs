using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface ISalaryRepository:IRepository<Salary>
{
    Task<Salary?> GetCurrentSalaryAsync(Guid employeeId, CancellationToken ct);
    
    Task<decimal> GetTotalCurrentSalaryAsync(CancellationToken ct);
    Task<IEnumerable<Salary>> GetHistoryAsync(Guid employeeId, CancellationToken ct);
    
    Task CloseAsync (Guid salaryId, CancellationToken ct);
}