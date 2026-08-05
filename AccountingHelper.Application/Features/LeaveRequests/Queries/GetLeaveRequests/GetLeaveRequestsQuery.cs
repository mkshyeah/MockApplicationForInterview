using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequests;

public record GetLeaveRequestsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<LeaveRequest>>;