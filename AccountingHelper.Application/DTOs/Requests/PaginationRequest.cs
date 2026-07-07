using System.ComponentModel.DataAnnotations;

namespace AccountingHelper.Application.DTOs.Requests;

public class PaginationRequest
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 10;
}