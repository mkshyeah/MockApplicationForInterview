using System.ComponentModel.DataAnnotations;
using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class CreateEmployeeRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required Guid PositionId { get; set; }
    public required decimal Salary { get; set; }
    public required SalaryType SalaryType { get; set; }
    public required Guid DepartmentId { get; set; }
    public required DateTime HireDate { get; set; }
}