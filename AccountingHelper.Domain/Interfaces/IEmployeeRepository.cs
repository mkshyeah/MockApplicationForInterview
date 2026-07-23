using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    
    Task<EmployeeStatus?> GetStatusAsync(Guid employeeId, CancellationToken ct);

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetFilteredAsync(
        int offset,
        int limit,
        EmployeeOrderBy orderBy,
        SortDirection direction,
        Guid? departmentId,
        EmployeeStatus? status,
        CancellationToken ct);
}