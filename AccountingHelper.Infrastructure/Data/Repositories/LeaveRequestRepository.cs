using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly ApplicationDbContext _dbContext;
    public LeaveRequestRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;


    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.LeaveRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        var entities = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.RequestedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        
        return entities.Select(x => x.ToModel()).ToList();
    }

    public void Add(LeaveRequest leaveRequest)
    {
        _dbContext.LeaveRequests.Add(leaveRequest.ToEntity());
    }

    public async Task ApplyDecisionAsync(LeaveRequest model, CancellationToken ct)
    {
        var entity = await _dbContext.LeaveRequests.
                FirstOrDefaultAsync(x => x.Id == model.Id, ct);

        if (entity is null)
            throw new InvalidOperationException($"Leave request '{model.Id}' was not found for update.");

        entity.Status = model.Status;
        entity.DecidedAt = model.DecidedAt;
        entity.DecidedBy = model.DecidedBy;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}