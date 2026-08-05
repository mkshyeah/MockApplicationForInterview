using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class LeaveBalanceMapping
{
    public static LeaveBalanceResponse ToResponse(this LeaveBalance leaveBalance) => new()
    {
        Id = leaveBalance.Id,
        EmployeeId = leaveBalance.EmployeeId,
        LeaveType = leaveBalance.LeaveType,
        RemainingDays = leaveBalance.RemainingDays,
    };
}