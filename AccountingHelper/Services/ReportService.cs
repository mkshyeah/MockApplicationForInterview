using AccountingHelper.Contexts;
using AccountingHelper.Models;
using AccountingHelper.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _dbContext;

    public ReportService(ApplicationDbContext dbContext)
    {
        _dbContext= dbContext;
    }

    public async Task<int> CountEmployees(CancellationToken ct)
    {
        return await _dbContext.Employees.CountAsync(ct);
    }

    public async Task<EmployeeStatus> GetEmployeeStatus(Guid id, CancellationToken ct)
    {
        var status = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (EmployeeStatus?) e.Status)
            .FirstOrDefaultAsync(ct);
        
        if(status == null)
            throw new KeyNotFoundException($"Employee with id {id} not found");
        
        return status.Value;
    }

    public async Task<decimal> GetTotalSalaries(CancellationToken ct)
    {
        var totalSalaries = await _dbContext.Employees.SumAsync(e => e.Salary,ct);

        return totalSalaries;
    }

    public async Task<decimal> GetSalaryByType(Guid id, string type, CancellationToken ct)
    {
        var salary = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (decimal?)e.Salary)
            .FirstOrDefaultAsync(ct);
        
        if (salary == null)
            throw new KeyNotFoundException($"Employee with id {id} not found");
        
        return type switch
        {
            "hourly" => salary.Value / 2080,
            "monthly" => salary.Value / 12,
            "weekly" => salary.Value / 52,
            "daily" => salary.Value/ 365,
            _ => throw new ArgumentException("Invalid salary type")
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
    public async Task<decimal> CalculateTaxes(Guid id, CancellationToken ct)
    {
        var salary = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (decimal?)e.Salary)
            .FirstOrDefaultAsync(ct);
        
        if (salary == null)
            throw new KeyNotFoundException($"Employee with id {id} not found");

        return salary switch
        {
            <= 600000m => salary.Value * 0.1m,
            <= 800000m => salary.Value * 0.2m,
            _          => salary.Value* 0.3m 
        };
    }
}