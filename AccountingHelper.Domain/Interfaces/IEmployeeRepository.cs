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
    // Batch form of ExistsByEmailAsync for the import: one query per batch instead of one per row.
    // Emails are compared as given, so callers normalize first.
    Task<IReadOnlySet<string>> GetExistingEmailsAsync(IReadOnlyCollection<string> emails, CancellationToken ct);
    
    Task<EmployeeStatus?> GetStatusAsync(Guid employeeId, CancellationToken ct);
    void Add(Employee model);
    Task ApplyStatusAsync(Employee model, CancellationToken ct);
    Task<IReadOnlyList<DepartmentHeadcount>> GetHeadcountByDepartmentAsync(CancellationToken ct);
    Task<IReadOnlyList<PayrollEmployeeSnapshot>> GetPayrollEmployeeSnapshotsAsync(CancellationToken ct);
}