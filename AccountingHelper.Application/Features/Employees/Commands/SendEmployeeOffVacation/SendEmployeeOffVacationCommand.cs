using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOffVacation;

public record SendEmployeeOffVacationCommand(Guid EmployeeId) : IRequest<Employee>;