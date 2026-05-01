namespace AccountingHelper.DTOs.Responses;

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Department { get; set; }
    public required string Position { get; set; }
    public required string Status { get; set; }
}