namespace AccountingHelper.Application.DTOs.Responses;

public class DepartmentResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; set; }
}