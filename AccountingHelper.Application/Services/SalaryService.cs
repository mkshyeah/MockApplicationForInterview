using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Services;

public class SalaryService : ISalaryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalaryService> _logger;
    
    public SalaryService(IUnitOfWork unitOfWork, ILogger<SalaryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Salary> ChangeSalary(Guid employeeId, SalaryType salaryType, decimal newSalary, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(employeeId, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", employeeId);
        
        if (employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException($"Cannot change salary of a fired employee with ID '{employeeId}'.");
        
        if (newSalary <= 0)
            throw new ValidationException(nameof(newSalary), "Salary must be greater than 0.");

        var currentSalary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(employeeId, ct);
        
        var oldSalaryAmount = currentSalary?.Amount;
        var oldSalaryType = currentSalary?.Type;
        
        if (currentSalary != null)
            await _unitOfWork.Salaries.CloseAsync(currentSalary.Id, ct);

        var salary = new Salary
        {
            Id = Guid.NewGuid(),
            Type = salaryType,
            Amount = newSalary,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employee.Id
        };
        
        _unitOfWork.Salaries.Add(salary);
        await _unitOfWork.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Salary updated for employee {EmployeeId}. Old: {OldAmount} ({OldType}), New: {NewAmount} ({NewType}).",
            employeeId,
            oldSalaryAmount,
            oldSalaryType,
            salary.Amount,
            salary.Type);
        
        return salary;
    }

    public async Task<IReadOnlyList<Salary>> GetSalaryHistory(Guid employeeId, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(employeeId, ct);
        
        if (status == null)
            throw new NotFoundException("Employee", employeeId);
        
        var salaries = await _unitOfWork.Salaries
            .GetHistoryAsync(employeeId, ct);
        
        return salaries.ToList().AsReadOnly();
    }
}