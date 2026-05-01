using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class Employee
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    public required string Position { get; set; }

    public decimal Salary { get; set; }

    public required string Department { get; set; }

    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    public EmployeeStatus Status { get; set; }
}