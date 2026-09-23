using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    public IPayrollRunRepository PayrollRuns { get; }
    public IPayslipRepository Payslips { get; }
    public IImportJobRepository ImportJobs { get; }

    public UnitOfWork(
        ApplicationDbContext dbContext,
        IEmployeeRepository employees,
        ISalaryRepository salaries,
        IDepartmentRepository departments,
        IPositionRepository positions,
        ILeaveRequestRepository leaveRequests,
        ILeaveBalanceRepository leaveBalances,
        IPayrollRunRepository payrollRuns,
        IPayslipRepository payslips,
        IImportJobRepository importJobs)
    {
        _dbContext = dbContext;
        Employees = employees;
        Salaries = salaries;
        Departments = departments;
        Positions = positions;
        LeaveRequests = leaveRequests;
        LeaveBalances = leaveBalances;
        PayrollRuns = payrollRuns;
        Payslips = payslips;
        ImportJobs = importJobs;
    }
    
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        // 23505 is unique_violation. Translated here because this is the only layer allowed to know
        // about Npgsql; what the violation means is for the caller to decide.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } postgres)
        {
            throw new UniqueConstraintViolationException(postgres.ConstraintName, ex);
        }
    }

    public void ClearTracking() => _dbContext.ChangeTracker.Clear();
}