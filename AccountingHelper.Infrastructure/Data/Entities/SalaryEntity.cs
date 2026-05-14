using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class SalaryEntity
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public SalaryType Type { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public Guid EmployeeId { get; set; }
    public EmployeeEntity? Employee { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}