namespace AccountingHelper.Domain.Models;

public class Payslip
{
    public Guid Id { get; set; }
    public required Guid PayrollRunId { get; set; }
    public required Guid EmployeeId { get; set; }
    
    public required decimal Gross { get; set; }
    public required decimal Tax { get; set; }
    public required decimal Deductions { get; set; }
    public required decimal Net { get; set; }
    
    public PayrollRun? PayrollRun { get; set; }
    public Employee? Employee { get; set; }
}