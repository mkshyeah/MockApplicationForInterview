using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Application.Features.Employees.Queries.GetEmployees;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class EmployeeMapping
{
    public static EmployeeResponse ToResponse(this Employee model) => new()
    {
        Id = model.Id,
        FullName = $"{model.FirstName} {model.LastName}",
        Email = model.Email,
        Department = model.Department?.Name,
        Position = model.Position?.Title,
        Status = model.Status.ToString(),
        CurrentSalary = model.Salaries
            .Where(s => s.EndDate == null)
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefault()?.Amount,
        HireDate = model.HireDate,
        TerminationDate =  model.TerminationDate
    };

    public static CreateEmployeeCommand ToCommand(this CreateEmployeeRequest request) => new(
        FirstName: request.FirstName,
        LastName: request.LastName,
        Email: request.Email,
        SalaryAmount: request.Salary,
        SalaryType: request.SalaryType,
        PositionId: request.PositionId,
        DepartmentId: request.DepartmentId,
        HireDate: request.HireDate);

    public static GetEmployeesQuery ToQuery(this EmployeeFilteredRequest request) => new(
        Offset: request.Offset,
        Limit: request.Limit,
        OrderBy: request.OrderBy,
        Direction: request.Direction,
        DepartmentId: request.DepartmentId,
        EmployeeStatus: request.EmployeeStatus);
}

