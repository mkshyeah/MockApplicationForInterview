using System.ComponentModel.DataAnnotations;

namespace AccountingHelper.Application.DTOs.Requests;

public class PaginationRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be >= 0")]
    public int Offset { get; set; } = 0;
    
    [Range(1, 100, ErrorMessage = "Count must be between 1 and 100")]
    public int Limit { get; set; } = 10;
}