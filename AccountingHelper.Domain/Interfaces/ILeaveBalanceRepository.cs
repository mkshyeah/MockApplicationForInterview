using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface ILeaveBalanceRepository
{
    Task<IReadOnlyList<LeaveBalance>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct);
    Task<(DebitOutcome Outcome, int RemainingDays)> DebitAsync(Guid employeeId, LeaveType type, int days, CancellationToken ct);
    void Add(LeaveBalance balance);
}