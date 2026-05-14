using System.ComponentModel.DataAnnotations;
using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class CreateEmployeeRequest
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required Guid PositionId { get; set; }

    [Required]
    [Range(0, 10_000_000)]
    public required decimal Salary { get; set; }
    
    [Required]
    public required SalaryType SalaryType { get; set; }

    [Required]
    public required Guid DepartmentId { get; set; }
    
    [Required]
    public required DateTime HireDate { get; set; }
}