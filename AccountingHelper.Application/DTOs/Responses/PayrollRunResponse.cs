using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class PayrollRunResponse
{
    public Guid Id { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public int ProcessedCount { get; init; }
    public int SkippedCount { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required PayrollRunStatus Status { get; init; }
}