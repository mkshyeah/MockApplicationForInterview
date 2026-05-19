using System.ComponentModel.DataAnnotations;
using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class ChangeSalaryRequest
{
    [Required]
    [Range(0.01, 10_000_000)]
    public decimal Amount { get; set; }
    
    [Required]
    public SalaryType SalaryType { get; set; }
}