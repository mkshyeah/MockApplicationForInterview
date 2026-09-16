using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class LeaveRequestResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public LeaveType LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public LeaveStatus Status { get; init; }
    public DateTime RequestedAt { get; init; }
    public int DurationInDays { get; init; }
    public DateTime? DecidedAt { get; init; }
    public Guid? DecidedBy { get; init; }
}