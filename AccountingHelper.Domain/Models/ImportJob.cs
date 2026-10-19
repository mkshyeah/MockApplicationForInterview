using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class ImportJob
{
    public Guid Id { get; set; }
    public required string FileName { get; set; } 
    public required ImportJobStatus Status { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public const int FileNameMaxLength = 255;
}