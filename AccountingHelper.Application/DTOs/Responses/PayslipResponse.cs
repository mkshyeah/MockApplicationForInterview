namespace AccountingHelper.Application.DTOs.Responses;

public class PayslipResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal Gross {get; init;}
    public decimal Tax {get; init;}
    public decimal Deductions {get; init;}
    public decimal Net {get; init;}
}