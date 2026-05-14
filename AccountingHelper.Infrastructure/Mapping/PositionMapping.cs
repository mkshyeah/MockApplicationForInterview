using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class PositionMapping
{
    public static Position ToModel(this PositionEntity entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Grade = entity.Grade,
    };

    public static PositionEntity ToEntity(this Position model) => new()
    {
        Id = model.Id,
        Title = model.Title,
        Description = model.Description,
        Grade = model.Grade,
    };
}