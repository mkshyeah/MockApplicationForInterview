using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
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

    public static Employee ToModel(this CreateEmployeeRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        PositionId = request.PositionId,
        DepartmentId = request.DepartmentId,
        HireDate = request.HireDate,
        Status = EmployeeStatus.Active,
        Salaries =
        [
            new Salary
            {
                Amount = request.Salary,
                Type = request.SalaryType,
                EffectiveDate = request.HireDate
            }
        ]
    };
}

