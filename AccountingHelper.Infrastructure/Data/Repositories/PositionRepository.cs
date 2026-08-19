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

    public async Task<Position?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken ct)
    {
        // Порядок по названию должности, а не по грейду: справочник отдаётся as-is,
        // а сортировку по грейду фронт при желании сделает сам.
        var entities = await _dbContext.Positions
            .AsNoTracking()
            .OrderBy(p => p.Title)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return entities.Select(e => e.ToModel()).ToList();
    }
}