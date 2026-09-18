using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class ImportJobEntity : ITimestamped
{
    public Guid Id { get; set; }
    public required string FileName { get; set; } 
    public ImportJobStatus Status { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}