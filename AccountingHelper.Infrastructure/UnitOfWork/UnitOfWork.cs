using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Infrastructure.Contexts;

namespace AccountingHelper.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    
    public IEmployeeRepository Employees { get; }
    public ISalaryRepository Salaries { get; }
    public IDepartmentRepository Departments { get; }
    public IPositionRepository Positions { get; }
    public ILeaveRequestRepository LeaveRequests { get; }
    public ILeaveBalanceRepository LeaveBalances { get; }

    public UnitOfWork(
        ApplicationDbContext dbContext,
        IEmployeeRepository employees,
        ISalaryRepository salaries,
        IDepartmentRepository departments,
        IPositionRepository positions,
        ILeaveRequestRepository leaveRequests,
        ILeaveBalanceRepository leaveBalances)
    {
        _dbContext = dbContext;
        Employees = employees;
        Salaries = salaries;
        Departments = departments;
        Positions = positions;
        LeaveRequests = leaveRequests;
        LeaveBalances = leaveBalances;
    }
    
    public async Task SaveChangesAsync(CancellationToken ct) 
        => await _dbContext.SaveChangesAsync(ct);
}