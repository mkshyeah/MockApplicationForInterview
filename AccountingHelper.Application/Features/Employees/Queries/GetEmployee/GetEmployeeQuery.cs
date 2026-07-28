using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployee;

public record GetEmployeeQuery(Guid EmployeeId) : IRequest<Employee>;