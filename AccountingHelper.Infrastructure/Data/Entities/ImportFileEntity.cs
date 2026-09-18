namespace AccountingHelper.Infrastructure.Data.Entities;

public class ImportFileEntity : ITimestamped
{
    public Guid Id { get; set; }
    public Guid ImportJobId { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}