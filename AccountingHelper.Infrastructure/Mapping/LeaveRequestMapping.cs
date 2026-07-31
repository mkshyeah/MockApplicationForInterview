using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class LeaveRequestMapping
{
    public static LeaveRequest ToModel(this LeaveRequestEntity entity) => new()
    {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        Employee = entity.Employee?.ToModel(),
        LeaveType = entity.LeaveType,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        Status = entity.Status,
        RequestedAt = entity.RequestedAt,
        DecidedAt =  entity.DecidedAt,
        DecidedBy =  entity.DecidedBy
    };

    public static LeaveRequestEntity ToEntity(this LeaveRequest model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        LeaveType = model.LeaveType,
        StartDate = model.StartDate,
        EndDate = model.EndDate,
        Status = model.Status,
        RequestedAt = model.RequestedAt.ToUniversalTime(),
        DecidedAt = model.DecidedAt?.ToUniversalTime(),
        DecidedBy = model.DecidedBy
    };
}