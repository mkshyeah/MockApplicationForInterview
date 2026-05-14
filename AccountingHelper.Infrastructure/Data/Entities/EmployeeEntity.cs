using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class EmployeeEntity
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }
    
    public Guid PositionId { get; set; }
    public PositionEntity? Position { get; set; }

    public Guid DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }
    
    public ICollection<SalaryEntity> Salaries { get; set; } = [];

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    public EmployeeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}