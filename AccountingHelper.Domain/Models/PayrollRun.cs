using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class PayrollRun
{
    public Guid Id { get; set; }
    public required int Year { get; set; }
    public required int Month { get; set; }
    public required PayrollRunStatus Status { get; set; }
    
    public int ProcessedCount { get; set; }
    public int SkippedCount { get; set; }
    
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public ICollection<Payslip> Payslips { get; set; } = [];
}