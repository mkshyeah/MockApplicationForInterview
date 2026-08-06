using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct);
    void Add(LeaveRequest leaveRequest);
    Task ApplyDecisionAsync(LeaveRequest model, CancellationToken ct);
}