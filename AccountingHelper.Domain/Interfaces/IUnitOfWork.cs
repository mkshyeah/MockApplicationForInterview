namespace AccountingHelper.Domain.Interfaces;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    ISalaryRepository Salaries { get; }
    IDepartmentRepository Departments { get; }
    IPositionRepository Positions { get; }
    Task SaveChangesAsync(CancellationToken ct);
}