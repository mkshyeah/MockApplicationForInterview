using System.ComponentModel.DataAnnotations;

namespace AccountingHelper.DTOs.Requests;

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
    [MaxLength(100)]
    public required string Position { get; set; }

    [Range(0, 10_000_000)]
    public decimal Salary { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Department { get; set; }
    
    public DateTime HireDate { get; set; }
}