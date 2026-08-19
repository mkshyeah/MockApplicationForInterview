using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.FireEmployee;

public record FireEmployeeCommand(Guid Id) : IRequest<EmployeeResponse>;