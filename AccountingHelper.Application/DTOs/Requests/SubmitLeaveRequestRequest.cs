using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class SubmitLeaveRequestRequest
{
    public LeaveType LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}