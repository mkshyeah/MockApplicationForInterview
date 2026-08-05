using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class LeaveRequestResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public int DurationInDays { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
}