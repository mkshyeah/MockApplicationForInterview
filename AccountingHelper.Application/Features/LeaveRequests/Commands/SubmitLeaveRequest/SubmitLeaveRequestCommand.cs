using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public record SubmitLeaveRequestCommand(
    Guid EmployeeId,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate
    ) : IRequest<LeaveRequest>;