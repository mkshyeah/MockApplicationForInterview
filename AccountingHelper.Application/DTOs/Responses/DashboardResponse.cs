namespace AccountingHelper.Application.DTOs.Responses;

public class DashboardResponse
{
    public decimal TotalMonthlyPayroll { get; init; }
    public decimal TotalMonthlyTax { get; init; }
    public IReadOnlyList<DepartmentHeadcountResponse> HeadcountByDepartment { get; init; } = [];
    public IReadOnlyList<GradeAverageSalaryResponse> AverageSalaryByGrade { get; init; } = [];
}