using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class ImportJobMapping
{
    public static ImportJob ToModel(this ImportJobEntity entity) => new()
    {
        Id = entity.Id,
        FileName = entity.FileName,
        Status = entity.Status,
        TotalRows = entity.TotalRows,
        ImportedCount = entity.ImportedCount,
        FailedCount = entity.FailedCount,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt
    };

    public static ImportJobEntity ToEntity(this ImportJob model) => new()
    {
        Id = model.Id,
        FileName = model.FileName,
        Status = model.Status,
        TotalRows = model.TotalRows,
        ImportedCount = model.ImportedCount,
        FailedCount = model.FailedCount,
        StartedAt = model.StartedAt,
        CompletedAt = model.CompletedAt
    };
}