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
    IImportJobRepository ImportJobs { get; }
    Task SaveChangesAsync(CancellationToken ct);
    // Detaches everything the context tracks. Batch processing calls it at every batch boundary:
    // after a failed save the rolled-back rows would ride along with the next one, after a
    // successful save they would slow every DetectChanges for the rest of the file.
    void ClearTracking();
}