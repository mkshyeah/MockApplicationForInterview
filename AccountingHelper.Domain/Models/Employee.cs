using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class Employee
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    
    public Guid PositionId { get; set; }
    public Position? Position { get; set; }
    
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Salary> Salaries { get; set; } = [];

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public required EmployeeStatus Status { get; set; }
}