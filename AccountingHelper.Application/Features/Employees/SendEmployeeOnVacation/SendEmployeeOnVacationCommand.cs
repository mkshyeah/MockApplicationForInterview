using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.SendEmployeeOnVacation;

public record SendEmployeeOnVacationCommand(Guid EmployeeId) : IRequest<Employee>;