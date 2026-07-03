using System.ComponentModel.DataAnnotations;
using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class ChangeSalaryRequest
{
    public decimal Amount { get; set; }
    public SalaryType SalaryType { get; set; }
}