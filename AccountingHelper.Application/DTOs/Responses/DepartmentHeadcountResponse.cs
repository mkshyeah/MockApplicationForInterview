namespace AccountingHelper.Application.DTOs.Responses;

public class DepartmentHeadcountResponse
{
    public Guid DepartmentId { get; init; }
    public required string DepartmentName { get; init; }
    public int Headcount { get; init; }
}