using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetAsync(Guid employeeId, LeaveType type, CancellationToken ct);
    Task<IReadOnlyList<LeaveBalance>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct);
    Task DebitAsync(Guid employeeId, LeaveType type, int days, CancellationToken ct);
    void Add(LeaveBalance balance);
}