using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly ApplicationDbContext _dbContext;
    public LeaveBalanceRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
    
    public async Task<LeaveBalance?> GetAsync(Guid employeeId, LeaveType type, CancellationToken ct)
    {
        var entity = await _dbContext.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveType == type, ct);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyList<LeaveBalance>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        var entities = await _dbContext.LeaveBalances
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.LeaveType)
            .ToListAsync(ct);
        
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task DebitAsync(Guid employeeId, LeaveType type, int days, CancellationToken ct)
    {
        var entity = await _dbContext.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveType == type, ct);

        if (entity == null)
            throw new InvalidOperationException($"No leave balance for employee {employeeId} and type {type}");
        
        entity.RemainingDays -= days;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public void Add(LeaveBalance balance)
    {
        _dbContext.LeaveBalances.Add(balance.ToEntity());
    }
}