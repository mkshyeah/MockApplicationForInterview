namespace AccountingHelper.Application.DTOs.Responses;

public class ImportErrorResponse
{
    public required int RowNumber { get; init; }
    public required string ErrorMessage { get; init; }
}