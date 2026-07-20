using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class Position
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string?  Description { get; set; }
    public required EmployeeGrade Grade { get; set; }
    public ICollection<Employee> Employees { get; set; } = [];
}