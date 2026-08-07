using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

public record RejectLeaveRequestCommand(Guid LeaveRequestId) : IRequest<LeaveRequest>;
