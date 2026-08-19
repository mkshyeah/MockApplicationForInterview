using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public record SubmitLeaveRequestCommand(
    Guid EmployeeId,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate
    ) : IRequest<LeaveRequestResponse>;