using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class SalaryResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public SalaryType Type { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
}