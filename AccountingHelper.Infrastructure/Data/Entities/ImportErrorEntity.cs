namespace AccountingHelper.Infrastructure.Data.Entities;

public class ImportErrorEntity : ITimestamped
{
    public Guid Id { get; set; }
    public Guid ImportJobId { get; set; }
    public int RowNumber { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}