using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class LeaveBalanceMapping
{
    public static LeaveBalance ToModel(this LeaveBalanceEntity entity) => new()
    {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        LeaveType = entity.LeaveType,
        RemainingDays = entity.RemainingDays
    };

    public static LeaveBalanceEntity ToEntity(this LeaveBalance model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        LeaveType = model.LeaveType,
        RemainingDays = model.RemainingDays
    };
}