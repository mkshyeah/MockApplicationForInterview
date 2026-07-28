using AccountingHelper.Application.Features.Pagination;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery(
    int Offset,
    int Limit,
    EmployeeOrderBy OrderBy,
    SortDirection Direction,
    Guid? DepartmentId,
    EmployeeStatus? EmployeeStatus) : IRequest<GetEmployeesResult>, IPagedQuery;

public record GetEmployeesResult(IReadOnlyList<Employee> Items, int TotalCount);