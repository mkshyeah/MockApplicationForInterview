using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct);
}