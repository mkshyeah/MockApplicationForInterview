using AccountingHelper.Contexts;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Services.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _dbContext;

    public ReportService(ApplicationDbContext dbContext)
    {
        _dbContext= dbContext;
    }

    public async Task<ErrorOr<int>> CountEmployees(CancellationToken ct)
    {
        return await _dbContext.Employees.CountAsync(ct);
    }

    public async Task<ErrorOr<EmployeeStatus>> GetEmployeeStatus(Guid id, CancellationToken ct)
    {
        var status = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (EmployeeStatus?) e.Status)
            .FirstOrDefaultAsync(ct);
        
        if(status == null)
            return Error.NotFound("Employee.NotFound",$"Employee with id {id} not found");
        
        return status.Value;
    }

    public async Task<ErrorOr<decimal>> GetTotalSalaries(CancellationToken ct)
    {
        var totalSalaries = await _dbContext.Employees.SumAsync(e => e.Salary,ct);

        return totalSalaries;
    }

    public async Task<ErrorOr<decimal>> GetSalaryByType(Guid id, SalaryType type, CancellationToken ct)
    {
        var salary = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (decimal?)e.Salary)
            .FirstOrDefaultAsync(ct);
        
        if (salary == null)
            return Error.NotFound("Employee.NotFound",$"Employee with id {id} not found");
        
        return type switch
        {
            SalaryType.Hourly => salary.Value / 2080,
            SalaryType.Monthly => salary.Value / 12,
            SalaryType.Weekly => salary.Value / 52,
            SalaryType.Daily => salary.Value/ 365,
            _ => Error.Validation("Salary.Invalid", "Invalid salary type")
        };
    }

    /// <summary>
    /// This method calculates the taxes for an employee based on their salary. The tax rates are as follows:
    /// - For salaries up to 600,000: 10%
    /// - For salaries between 600,001 and 800,000: 20%
    /// - For salaries more than 800,001: 30%
    /// </summary>
    /// <param name="employee"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<ErrorOr<decimal>> CalculateTaxes(Guid id, CancellationToken ct)
    {
        var salary = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (decimal?)e.Salary)
            .FirstOrDefaultAsync(ct);
        
        if (salary == null)
            return Error.NotFound("Employee.NotFound",$"Employee with id {id} not found");

        return salary switch
        {
            <= 600000m => salary.Value * 0.1m,
            <= 800000m => salary.Value * 0.2m,
            _          => salary.Value* 0.3m 
        };
    }
}