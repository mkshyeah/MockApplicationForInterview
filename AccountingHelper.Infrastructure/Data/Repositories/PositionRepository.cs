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
}