using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Infrastructure.Data.Entities;

public class PositionEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string?  Description { get; set; }
    public required EmployeeGrade Grade { get; set; }
    
    public ICollection<EmployeeEntity> Employees { get; set; } = [];
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}