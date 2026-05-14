using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class EmployeeMapping
{
    public static Employee ToModel(this EmployeeEntity entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        Department = entity.Department?.ToModel(),
        DepartmentId = entity.DepartmentId,
        Position = entity.Position?.ToModel(),
        PositionId = entity.PositionId,
        Salaries = entity.Salaries.Select(e => e.ToModel()).ToList() ?? [],
        HireDate = entity.HireDate,
        TerminationDate = entity.TerminationDate,
        Status =  entity.Status
    };

    public static EmployeeEntity ToEntity(this Employee model) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Email = model.Email,
        DepartmentId = model.DepartmentId,
        PositionId = model.PositionId,
        Salaries = model.Salaries.Select(e => e.ToEntity()).ToList(),
        HireDate = model.HireDate,
        TerminationDate = model.TerminationDate,
        Status = model.Status
    };
}