using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOnVacation;

public record SendEmployeeOnVacationCommand(Guid EmployeeId) : IRequest<EmployeeResponse>;