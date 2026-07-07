using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Services;

public class ReportService : IReportService
{
    private const int HoursInYear   = 2080;
    private const int MonthsInYear  = 12;
    private const int WeeksInYear   = 52;
    private const int DaysInYear    = 365;
  
    private const decimal LowTaxBracket  = 600_000m;
    private const decimal HighTaxBracket = 800_000m;
    private const decimal LowTaxRate     = 0.10m;
    private const decimal MidTaxRate     = 0.20m;
    private const decimal HighTaxRate    = 0.30m;
    
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CountEmployees(CancellationToken ct)
    {
        return await _unitOfWork.Employees.CountAsync(ct);
    }

    public async Task<EmployeeStatus> GetEmployeeStatus(Guid employeeId, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees
            .GetStatusAsync(employeeId, ct);
        
        if(!status.HasValue)
           throw new NotFoundException($"Employee with id {employeeId} not found");
        
        return status.Value;
    }

    public async Task<decimal> GetTotalSalaries(CancellationToken ct)
    {
        return await _unitOfWork.Salaries
            .GetTotalCurrentSalaryAsync(ct);
    }

    public async Task<decimal> GetSalaryByType(Guid employeeId, SalaryType type, CancellationToken ct)
    {
        var salary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(employeeId, ct);
        
        if (salary == null)
            throw new NotFoundException("Employee Salary", employeeId);
    
        var annualSalary = salary.Type switch
        {
            SalaryType.Hourly => salary.Amount * HoursInYear,
            SalaryType.Daily => salary.Amount * DaysInYear,
            SalaryType.Weekly => salary.Amount * WeeksInYear,
            SalaryType.Monthly => salary.Amount * MonthsInYear,
            _ => throw new BusinessRuleException($"Unsupported current salary type: {salary.Type}")
        };

        return type switch
        {
            SalaryType.Hourly => annualSalary / HoursInYear,
            SalaryType.Daily => annualSalary / DaysInYear,
            SalaryType.Weekly => annualSalary / WeeksInYear,
            SalaryType.Monthly => annualSalary / MonthsInYear,
            _ => throw new BusinessRuleException($"Requested salary type is invalid: {type}")
        };
    }
    
    public async Task<decimal> CalculateTaxes(Guid employeeId, CancellationToken ct)
    {
        var salary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(employeeId, ct);
        
        if (salary == null)
            throw new NotFoundException("Employee Salary", employeeId);

        var monthlyAmount = salary.Type switch
        {
            SalaryType.Monthly => salary.Amount,
            SalaryType.Hourly  => salary.Amount * HoursInYear / MonthsInYear,
            SalaryType.Daily   => salary.Amount * DaysInYear  / MonthsInYear,
            SalaryType.Weekly  => salary.Amount * WeeksInYear / MonthsInYear,
            _ => throw new BusinessRuleException($"Unsupported salary type: {salary.Type}")
        };

        return monthlyAmount switch
        {
            <= LowTaxBracket  => monthlyAmount * LowTaxRate,
            <= HighTaxBracket => monthlyAmount * MidTaxRate,
            _                 => monthlyAmount * HighTaxRate
        };
    }
}