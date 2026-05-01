using AccountingHelper.Domain.Models;
using AccountingHelper.DTOs.Requests;
using AccountingHelper.DTOs.Responses;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Extensions;

public static class EmployeeEntityExtensions
{
    public static Employee ToModel(this EmployeeEntity entity)
    {
        return new Employee
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            Position = entity.Position,
            Salary = entity.Salary,
            Department = entity.Department,
            HireDate = entity.HireDate,
            TerminationDate = entity.TerminationDate,
            Status = entity.Status
        };
    }

    public static EmployeeEntity ToEntity(this Employee model)
    {
        return new EmployeeEntity
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Position = model.Position,
            Salary = model.Salary,
            Department = model.Department,
            HireDate = model.HireDate,
            TerminationDate = model.TerminationDate,
            Status = model.Status
        };
    }

    public static Employee ToModel(this CreateEmployeeRequest request)
    {
        return new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Position = request.Position,
            Salary = request.Salary,
            Department = request.Department,
            HireDate = request.HireDate
        };
    }
    
    public static EmployeeResponse ToResponse(this Employee model)
    {
        return new EmployeeResponse
        {
            Id = model.Id,
            FullName = $"{model.FirstName} {model.LastName}",
            Email = model.Email,
            Department = model.Department,
            Position = model.Position,
            Status = model.Status.ToString(),
        };
    }
}