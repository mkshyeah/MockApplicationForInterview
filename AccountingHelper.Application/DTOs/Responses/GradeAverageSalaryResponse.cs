using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Responses;

public class GradeAverageSalaryResponse
{
    public EmployeeGrade Grade { get; init; }
    public decimal AverageMonthlySalary { get; init; }
}