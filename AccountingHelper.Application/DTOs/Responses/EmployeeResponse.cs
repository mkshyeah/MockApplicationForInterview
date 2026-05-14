namespace AccountingHelper.Application.DTOs.Responses;

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public required string Status { get; set; }
    public  decimal? CurrentSalary { get; set; }
    
    public DateTime HireDate { get; set; }
    
    public DateTime? TerminationDate { get; set; }
}