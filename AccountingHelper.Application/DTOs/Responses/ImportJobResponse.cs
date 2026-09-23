using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class ImportJobResponse
{
    public Guid Id { get; init; }
    public required string FileName { get; init; }
    public required ImportJobStatus Status { get; init; }
    public int TotalRows { get; init; }
    public int ImportedCount { get; init; }
    public int FailedCount { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}