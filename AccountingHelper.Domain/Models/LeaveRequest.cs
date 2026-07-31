using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public required Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public required LeaveType LeaveType { get; set; }
    public required DateOnly StartDate { get; set; }
    public required DateOnly EndDate { get; set; }
    public required LeaveStatus Status { get; set; }
    public required DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
    
    public int DurationInDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}