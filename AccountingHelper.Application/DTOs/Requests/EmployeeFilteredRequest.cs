using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Application.DTOs.Requests;

public class EmployeeFilteredRequest : PaginationRequest
{
    public EmployeeOrderBy OrderBy { get; set; } = EmployeeOrderBy.Name;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
    public Guid? DepartmentId { get; set; }
    public EmployeeStatus? EmployeeStatus { get; set; }
}