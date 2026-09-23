using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class ImportJobMapping
{
    public static ImportJobResponse ToResponse(this ImportJob model) => new()
    {
        Id = model.Id,
        FileName = model.FileName,
        TotalRows = model.TotalRows,
        ImportedCount = model.ImportedCount,
        FailedCount = model.FailedCount,
        Status = model.Status,
        StartedAt = model.StartedAt,
        CompletedAt = model.CompletedAt
    };

    public static ImportErrorResponse ToResponse(this ImportError model) => new()
    {
        RowNumber = model.RowNumber,
        ErrorMessage = model.Message
    };
}