using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    decimal SalaryAmount,
    SalaryType SalaryType,
    Guid PositionId,
    Guid DepartmentId,
    DateTime HireDate) : IRequest<EmployeeResponse>;