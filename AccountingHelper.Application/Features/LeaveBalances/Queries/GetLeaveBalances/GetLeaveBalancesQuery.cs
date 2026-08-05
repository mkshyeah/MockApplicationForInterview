using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveBalances.Queries.GetLeaveBalances;

public record GetLeaveBalancesQuery(Guid EmployeeId) : IRequest<IReadOnlyList<LeaveBalance>>;