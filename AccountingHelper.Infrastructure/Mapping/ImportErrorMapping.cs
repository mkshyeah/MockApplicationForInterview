using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class ImportErrorMapping
{
    public static ImportError ToModel(this ImportErrorEntity entity) => new()
    {
        Id = entity.Id,
        ImportJobId = entity.ImportJobId,
        RowNumber = entity.RowNumber,
        Message = entity.Message,
    };

    public static ImportErrorEntity ToEntity(this ImportError model) => new()
    {
        Id = model.Id,
        ImportJobId = model.ImportJobId,
        RowNumber = model.RowNumber,
        Message = model.Message
    };
}