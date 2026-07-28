using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Salaries.ChangeSalary;

public record ChangeSalaryCommand(
    Guid EmployeeId,
    decimal Amount,
    SalaryType SalaryType) : IRequest<Salary>;