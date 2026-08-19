using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

public record ApproveLeaveRequestCommand(Guid LeaveRequestId) : IRequest<LeaveRequestResponse>;