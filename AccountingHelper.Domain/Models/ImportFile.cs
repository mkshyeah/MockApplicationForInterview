namespace AccountingHelper.Domain.Models;

public class ImportFile
{
    public Guid Id { get; set; }
    public required Guid ImportJobId { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
}