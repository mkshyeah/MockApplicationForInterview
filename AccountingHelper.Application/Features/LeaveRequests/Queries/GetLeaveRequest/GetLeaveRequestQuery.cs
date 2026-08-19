using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequest;

public record GetLeaveRequestQuery(Guid Id) : IRequest<LeaveRequestResponse>;
