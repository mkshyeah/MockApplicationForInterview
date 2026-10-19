namespace AccountingHelper.Domain.Models;

public class ImportError
{
    public Guid Id { get; set; }
    public required Guid ImportJobId { get; set; }
    public required int RowNumber { get; set; }
    public required string Message { get; set; }
    
    public const int MessageMaxLength = 1000;
}