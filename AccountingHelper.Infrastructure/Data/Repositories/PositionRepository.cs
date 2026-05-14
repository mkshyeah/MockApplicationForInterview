using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly ApplicationDbContext _dbContext;
    public PositionRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
    
    public async Task<IEnumerable<Position>> GetAllAsync(int offset, int limit, SortDirection direction, CancellationToken ct)
    {
        var query = _dbContext.Positions
            .AsNoTracking();
        
        query = direction == SortDirection.Ascending
            ? query.OrderBy(p => p.CreatedAt)
            : query.OrderByDescending(p => p.CreatedAt);
        
        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(p => p.ToModel());

    }

    public async Task<Position?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity?.ToModel();
    }

    public void Add(Position model) => _dbContext.Positions.Add(model.ToEntity());

    public void Update(Position model)
    {
        var entity = model.ToEntity();
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.Positions.Update(entity);
        
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Positions.FindAsync([id], ct);
        if (entity != null)
            _dbContext.Positions.Remove(entity);
    }

    public async Task<int> CountAsync(CancellationToken ct)
    {
        return await _dbContext.Positions.CountAsync(ct);
    }
}