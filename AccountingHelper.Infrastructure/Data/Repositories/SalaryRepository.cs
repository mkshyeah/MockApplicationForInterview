using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class SalaryRepository : ISalaryRepository
{
    private readonly ApplicationDbContext _dbContext;
    public SalaryRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
    
    public async Task<IEnumerable<Salary>> GetAllAsync(int offset, int limit, SortDirection direction, CancellationToken ct)
    {
        var query = _dbContext.Salaries
            .Include(s => s.Employee)
            .AsNoTracking();
        
        query = direction == SortDirection.Ascending 
            ? query.OrderBy(s => s.CreatedAt)
            : query.OrderByDescending(s => s.CreatedAt);
        
        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(s => s.ToModel());
    }

    public async Task<Salary?> GetByIdAsync(Guid id, CancellationToken ct)
    {
         var entity = await _dbContext.Salaries
             .Include(s => s.Employee)
             .AsNoTracking()
             .FirstOrDefaultAsync(s => s.Id == id, ct);
         
         return entity?.ToModel();
    }

    public void Add(Salary model) => _dbContext.Salaries.Add(model.ToEntity());

    public void Update(Salary model)
    {
        var entity = model.ToEntity(); 
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.Salaries.Update(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries.FindAsync([id], ct);
        if (entity != null)
            _dbContext.Salaries.Remove(entity);
        
    }

    public async Task<int> CountAsync(CancellationToken ct)
    {
        return await _dbContext.Salaries.CountAsync(ct);
    }

    public async Task<Salary?> GetCurrentSalaryAsync(Guid employeeId, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId && s.EndDate == null)
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        return entity?.ToModel();
    }

    public async Task<decimal> GetTotalCurrentSalaryAsync(CancellationToken ct)
    {
        return await _dbContext.Salaries
            .Where(s => s.EndDate == null)
            .SumAsync(s => s.Amount, ct);
    }

    public async Task<IEnumerable<Salary>> GetHistoryAsync(Guid employeeId, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.EffectiveDate)
            .ToListAsync(ct);
        
        return entity.Select(s => s.ToModel());
    }

    public async Task CloseAsync(Guid salaryId, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries
            .FindAsync([salaryId], ct);

        if (entity != null)
        {
            entity.EndDate = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
        }

    }
}