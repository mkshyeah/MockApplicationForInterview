using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using MediatR;

namespace AccountingHelper.Application.Features.Salaries.Commands.ChangeSalary;

public record ChangeSalaryCommand(
    Guid EmployeeId,
    decimal Amount,
    SalaryType SalaryType) : IRequest<SalaryResponse>;