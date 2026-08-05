using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequest;

public record GetLeaveRequestQuery(Guid Id) : IRequest<LeaveRequest>;
