using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class LeaveRequestEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public EmployeeEntity? Employee { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}