using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class ImportFileMapping
{
    public static ImportFile ToModel(this ImportFileEntity entity) => new()
    {
        Id = entity.Id,
        ImportJobId = entity.ImportJobId,
        ContentType = entity.ContentType,
        Content = entity.Content
    };
    
    public static ImportFileEntity ToEntity(this ImportFile model) => new()
    {
        Id = model.Id,
        ImportJobId = model.ImportJobId,
        ContentType = model.ContentType,
        Content = model.Content
    };
}