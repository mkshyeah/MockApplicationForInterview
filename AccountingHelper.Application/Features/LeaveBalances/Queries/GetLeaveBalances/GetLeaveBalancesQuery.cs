using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveBalances.Queries.GetLeaveBalances;

public record GetLeaveBalancesQuery(Guid EmployeeId) : IRequest<IReadOnlyList<LeaveBalanceResponse>>;