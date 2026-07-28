using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOnVacation;

public record SendEmployeeOnVacationCommand(Guid EmployeeId) : IRequest<Employee>;