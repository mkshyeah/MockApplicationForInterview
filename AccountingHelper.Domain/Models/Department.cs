namespace AccountingHelper.Domain.Models;

public class Department
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Employee> Employees { get; set; } = [];
}