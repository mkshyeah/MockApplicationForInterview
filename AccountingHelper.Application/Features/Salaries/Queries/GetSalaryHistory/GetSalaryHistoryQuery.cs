using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Salaries.Queries.GetSalaryHistory;

public record GetSalaryHistoryQuery(Guid EmployeeId) : IRequest<IReadOnlyList<Salary>>;
