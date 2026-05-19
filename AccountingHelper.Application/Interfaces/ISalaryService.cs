using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Interfaces;

public interface ISalaryService
{
    Task<Salary> ChangeSalary(Guid employeeId, SalaryType salaryType, decimal newSalary, CancellationToken ct);
    
    Task<IReadOnlyList<Salary>> GetSalaryHistory(Guid employeeId, CancellationToken ct);
}