using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class DepartmentMapping
{
    public static Department ToModel(this DepartmentEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
    };

    public static DepartmentEntity ToEntity(this Department model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description
    };
}