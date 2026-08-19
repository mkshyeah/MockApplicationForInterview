using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IPositionRepository
{
    Task<Position?>GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken ct);
}
