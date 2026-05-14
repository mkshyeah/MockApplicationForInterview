using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _dbContext;
    public DepartmentRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
    
    public async Task<IEnumerable<Department>> GetAllAsync(int offset, int limit, SortDirection direction, CancellationToken ct)
    {
        var query = _dbContext.Departments
            .AsNoTracking();
        
        query = direction == SortDirection.Ascending
            ? query.OrderBy(d => d.CreatedAt)
            : query.OrderByDescending(d => d.CreatedAt);
        
        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(d => d.ToModel());
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        
        return entity?.ToModel();
    }

    public void Add(Department model) => _dbContext.Add(model.ToEntity());

    public void Update(Department model)
    {
        var entity = model.ToEntity();
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.Departments.Update(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Departments.FindAsync([id], ct);
        
        if(entity != null)
            _dbContext.Departments.Remove(entity);
    }

    public async Task<int> CountAsync(CancellationToken ct)
    {
        return await _dbContext.Departments.CountAsync(ct);
    }
}