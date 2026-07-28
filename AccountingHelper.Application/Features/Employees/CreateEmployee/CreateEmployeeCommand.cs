using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.CreateEmployee;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    decimal SalaryAmount,
    SalaryType SalaryType,
    Guid PositionId,
    Guid DepartmentId,
    DateTime HireDate) : IRequest<Employee>;