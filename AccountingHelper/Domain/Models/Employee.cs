using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public class Employee
{
    public Guid Id { get; set; }

    public string FirstName { get; set; }
    
    public string LastName { get; set; }

    public string Email { get; set; }

    public string Position { get; set; }

    public decimal Salary { get; set; }

    public string Department { get; set; }

    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    public EmployeeStatus Status { get; set; }
}