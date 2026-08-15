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

    public async Task<IReadOnlyList<LeaveBalance>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        var entities = await _dbContext.LeaveBalances
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.LeaveType)  
            .ToListAsync(ct);
        
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<(DebitOutcome Outcome, int RemainingDays)> DebitAsync(Guid employeeId, LeaveType type, int days, CancellationToken ct)
    {
        var entity = await _dbContext.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveType == type, ct);

        if (entity == null)
            return (DebitOutcome.BalanceNotFound, 0);

        // остаток до списания: он нужен для сообщения об отказе, после вычитания значение уже не то
        var remainingDays = entity.RemainingDays;
        if (remainingDays < days)
            return (DebitOutcome.InsufficientDays, remainingDays);

        entity.RemainingDays = remainingDays - days;
        return (DebitOutcome.Ok, remainingDays);
    }

    public void Add(LeaveBalance balance)
    {
        _dbContext.LeaveBalances.Add(balance.ToEntity());
    }
}