using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOffVacation;

public record SendEmployeeOffVacationCommand(Guid EmployeeId) : IRequest<EmployeeResponse>;