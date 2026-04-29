using AccountingHelper.Models;

namespace AccountingHelper.Extensions;

public static class EmployeeEntityExtensions
{
    public static Employee ToModel(this EmployeeEntity entity)
    {
        if(entity == null) return null;
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
        if (model == null) return null;
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
        if (request == null) return null;
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
        if (model == null) return null;
        return new EmployeeResponse
        {
            Id = model.Id,
            FullName = $"{model.FirstName} {model.LastName}",
            Position = model.Position,
            Status = model.Status.ToString(),
        };
    }
}