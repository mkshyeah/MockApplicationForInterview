using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class LeaveBalanceResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public int RemainingDays { get; set; }
}