using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.FireEmployee;

public record FireEmployeeCommand(Guid Id) : IRequest<Employee>;