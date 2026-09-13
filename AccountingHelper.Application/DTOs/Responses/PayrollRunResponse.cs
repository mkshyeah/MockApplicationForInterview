using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class PayrollRunResponse
{
    public Guid Id { get; set; }
    public required int Year { get; set; }
    public required int Month { get; set; }
    public required PayrollRunStatus Status { get; set; }
}