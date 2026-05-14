namespace AccountingHelper.Infrastructure.Data.Entities;

public class DepartmentEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public ICollection<EmployeeEntity> Employees { get; set; } = [];
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}