using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class EmployeeEntity
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
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }          
}