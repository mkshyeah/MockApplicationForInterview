using AccountingHelper.Contexts;
using AccountingHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Services;

public class ReportControllerService : IReportControllerService
{
    private readonly ApplicationDbContext _context;

    public ReportControllerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> CountEmployees()
    {
        return Task.FromResult(_context.Employees.Count());
    }

    public async Task<string> GetEmployeeStatus(string id)
    {
        var employee = await _context.Employees.Where(x => x.Id == id).FirstAsync();
        return employee.Status;
    }

    public async Task<int> GetTotalSalaries()
    {
        var totalSalaries = await _context.Employees.SumAsync(e => e.Salary);

        return (int)totalSalaries;
    }

    public async Task<decimal> GetSalaryByType(Employee employee, string type)
    {
        return type switch
        {
            "hourly" => employee.Salary / 2080,
            "monthly" => employee.Salary / 12,
            "weekly" => employee.Salary / 52,
            "daily" => employee.Salary / 365,
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
    public async Task<decimal> CalculateTaxes(Employee employee)
    {
        var salary = employee.Salary;

        return salary switch
        {
            <= 600000 => salary * 0.1m,
            <= 800000 => salary * 0.2m,
            >= 900000 => salary * 0.3m,
            _ => throw new ArgumentException("Invalid salary range")
        };
    }
}