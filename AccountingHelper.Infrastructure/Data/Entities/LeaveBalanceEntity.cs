using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class LeaveBalanceEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public int RemainingDays { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}