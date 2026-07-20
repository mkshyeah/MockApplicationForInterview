using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Services;

public class ReportService : IReportService
{
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
        
        if (!status.HasValue)
           throw new NotFoundException("Employee", employeeId);
        
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
    
        return salary.ConvertTo(type);
    }
    
    public async Task<decimal> CalculateTaxes(Guid employeeId, CancellationToken ct)
    {
        var salary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(employeeId, ct);
        
        if (salary == null)
            throw new NotFoundException("Employee Salary", employeeId);

        return salary.CalculateTaxes();
    }
}