using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

public record ApproveLeaveRequestCommand(Guid LeaveRequestId) : IRequest<LeaveRequest>;