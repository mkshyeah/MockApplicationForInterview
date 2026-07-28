using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Interfaces;

public interface ISalaryService
{
    Task<IReadOnlyList<Salary>> GetSalaryHistory(Guid employeeId, CancellationToken ct);
}