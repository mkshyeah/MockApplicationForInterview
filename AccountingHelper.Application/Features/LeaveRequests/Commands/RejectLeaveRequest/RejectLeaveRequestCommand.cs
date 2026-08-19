using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

public record RejectLeaveRequestCommand(Guid LeaveRequestId) : IRequest<LeaveRequestResponse>;
