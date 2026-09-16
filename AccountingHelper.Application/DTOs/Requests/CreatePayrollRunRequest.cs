namespace AccountingHelper.Application.DTOs.Requests;

public class CreatePayrollRunRequest
{
    public required int Year { get; set; }
    public required int Month { get; set; }
}