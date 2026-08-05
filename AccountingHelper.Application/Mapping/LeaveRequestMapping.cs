using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class LeaveRequestMapping
{
    public static LeaveRequestResponse ToResponse(this LeaveRequest model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        LeaveType = model.LeaveType,
        StartDate = model.StartDate,
        EndDate = model.EndDate,
        Status = model.Status,
        RequestedAt = model.RequestedAt,
        DurationInDays = model.DurationInDays,
        DecidedAt = model.DecidedAt,
        DecidedBy = model.DecidedBy
    };
    
    public static SubmitLeaveRequestCommand ToCommand(this SubmitLeaveRequestRequest request, Guid employeeId) => new(
        EmployeeId: employeeId,
        LeaveType: request.LeaveType,
        StartDate: request.StartDate,
        EndDate: request.EndDate);
}