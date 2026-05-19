using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Services;

public class SalaryService : ISalaryService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public SalaryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Salary> ChangeSalary(Guid employeeId, SalaryType salaryType, decimal newSalary, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(employeeId, ct);
        
        if (employee == null)
            throw new NotFoundException($"Employee with id {employeeId} not found");
        
        if (employee.Status == EmployeeStatus.Fired)
            throw new ConflictException($"Cannot change salary of fired employee with id {employeeId}");
        
        if (newSalary <= 0)
            throw new ValidationException("Salary must be greater than 0");

        var currentSalary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(employeeId, ct);
        
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
        
        return salary;
    }

    public async Task<IReadOnlyList<Salary>> GetSalaryHistory(Guid employeeId, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(employeeId, ct);
        if (status == null)
            throw new NotFoundException($"Employee with id {employeeId} not found");
        
        var salaries = await _unitOfWork.Salaries
            .GetHistoryAsync(employeeId, ct);
        
        return salaries.ToList().AsReadOnly();
    }
}