using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class Salary
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public SalaryType Type { get; set; }
    public DateTime EffectiveDate { get; set; } // С какого числа действует
    public DateTime? EndDate { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}