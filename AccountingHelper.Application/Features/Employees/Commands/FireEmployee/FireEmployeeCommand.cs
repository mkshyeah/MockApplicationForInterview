using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.FireEmployee;

public record FireEmployeeCommand(Guid Id) : IRequest<Employee>;