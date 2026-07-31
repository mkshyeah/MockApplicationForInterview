using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public required Guid EmployeeId { get; set; }
    public required LeaveType LeaveType { get; set; }
    public required int RemainingDays { get; set; }
}