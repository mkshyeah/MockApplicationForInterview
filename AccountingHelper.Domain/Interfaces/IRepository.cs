using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(
        int offset, int limit, SortDirection direction,
        CancellationToken ct);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(T model);
    void Update(T model);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}