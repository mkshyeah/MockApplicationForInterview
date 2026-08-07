using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetFilteredAsync(
        int offset,
        int limit,
        EmployeeOrderBy orderBy,
        SortDirection direction,
        Guid? departmentId,
        EmployeeStatus? status,
        CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    
    Task<EmployeeStatus?> GetStatusAsync(Guid employeeId, CancellationToken ct);
    void Add(Employee model);
    Task ApplyStatusAsync(Employee model, CancellationToken ct);
}