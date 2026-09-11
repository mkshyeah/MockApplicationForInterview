namespace AccountingHelper.Infrastructure.Data.Entities;

public class PayslipEntity : ITimestamped
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    
    public decimal Gross { get; set; }
    public decimal Tax { get; set; }
    public decimal Deductions { get; set; }
    public decimal Net { get; set; }
    
    public PayrollRunEntity? PayrollRun { get; set; }
    public EmployeeEntity? Employee { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}