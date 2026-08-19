using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Domain.Services;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class SalaryRepository : ISalaryRepository
{
    private readonly ApplicationDbContext _dbContext;
    public SalaryRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
    
    public void Add(Salary model) => _dbContext.Salaries.Add(model.ToEntity());

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
        var snapshots = await GetActiveSnapshotsAsync(ct);
        return snapshots.Sum(s => SalaryCalculator.Convert(s.Amount, s.Type, SalaryType.Monthly));
    }

    public async Task<IReadOnlyList<Salary>> GetHistoryAsync(Guid employeeId, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.EffectiveDate)
            .ToListAsync(ct);
        
        return entity.Select(s => s.ToModel()).ToList();
    }

    public async Task CloseAsync(Guid salaryId, CancellationToken ct)
    {
        var entity = await _dbContext.Salaries
            .FindAsync([salaryId], ct);

        if (entity != null)
        {
            entity.EndDate = DateTime.UtcNow;
        }
    }
    
    public async Task<IReadOnlyList<ActiveSalarySnapshot>> GetActiveSnapshotsAsync(CancellationToken ct)
    {
        return await _dbContext.Salaries
            .AsNoTracking()
            .Where(s => s.EndDate==null
                        && EmployeeStatuses.Employed.Contains(s.Employee!.Status))
            .Select(s => new ActiveSalarySnapshot(
                s.Amount,
                s.Type,
                s.Employee!.Position!.Grade))
            .ToListAsync(ct);
    }
}