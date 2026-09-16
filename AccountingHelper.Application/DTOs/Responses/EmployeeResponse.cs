namespace AccountingHelper.Application.DTOs.Responses;

public class EmployeeResponse
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Department { get; init; }
    public string? Position { get; init; }
    public required string Status { get; init; }
    public  decimal? CurrentSalary { get; init; }
    
    public DateTime HireDate { get; init; }
    
    public DateTime? TerminationDate { get; init; }
}