namespace AccountingHelper.Domain.Interfaces;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    ISalaryRepository Salaries { get; }
    IDepartmentRepository Departments { get; }
    IPositionRepository Positions { get; }
    ILeaveRequestRepository LeaveRequests { get; }
    ILeaveBalanceRepository LeaveBalances { get; }
    IPayrollRunRepository PayrollRuns { get; }
    IPayslipRepository Payslips { get; }
    Task SaveChangesAsync(CancellationToken ct);
}