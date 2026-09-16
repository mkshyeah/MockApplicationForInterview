using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class LeaveBalanceResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public LeaveType LeaveType { get; init; }
    public int RemainingDays { get; init; }
}