using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Services;

public class EmployeeService:IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IUnitOfWork unitOfWork, ILogger<EmployeeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetEmployees(EmployeeFilteredRequest request , CancellationToken ct )
    {
        var (employees,total) = await _unitOfWork.Employees.GetFilteredAsync(
            request.Offset,
            request.Limit,
            request.OrderBy,
            request.Direction,
            request.DepartmentId,
            request.EmployeeStatus,
            ct);

        return (employees, total);
    }

    public async Task<Employee> GetEmployee(Guid id, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(id, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", id);

        return employee;
    }

    public async Task<Employee> CreateEmployee(Employee employee, CancellationToken ct)
    {
        var exists = await _unitOfWork.Employees
            .ExistsByEmailAsync(employee.Email, ct);
        
        if (exists)
            throw new ConflictException($"Employee with email '{employee.Email}' already exists.");
        
        var department = await _unitOfWork.Departments.GetByIdAsync(employee.DepartmentId, ct);
        if (department == null)
            throw new NotFoundException("Department", employee.DepartmentId);
        
        var position = await _unitOfWork.Positions.GetByIdAsync(employee.PositionId, ct);
        if (position is null)
            throw new NotFoundException("Position", employee.PositionId);

        employee.Id = Guid.NewGuid();
        employee.Status = EmployeeStatus.Active;

        foreach (var salary in employee.Salaries)
        {
            salary.Id = Guid.NewGuid();
            salary.EmployeeId = employee.Id;
        }

        _unitOfWork.Employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(ct);

        return (await _unitOfWork.Employees.GetByIdAsync(employee.Id, ct))!;
    }

    public async Task<Employee> FireEmployee(Guid id, CancellationToken ct)
    {
        var employee= await _unitOfWork.Employees
            .GetByIdAsync(id, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", id);
        
        if (employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException($"Employee with ID '{id}' is already fired.");
        
        var currentSalary = await _unitOfWork.Salaries.GetCurrentSalaryAsync(id, ct);
        if (currentSalary != null)
            await _unitOfWork.Salaries.CloseAsync(currentSalary.Id, ct);
            
        employee.TerminationDate = DateTime.UtcNow;
        employee.Status = EmployeeStatus.Fired;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        _logger.LogInformation("Employee {EmployeeId} was successfully fired. Associated active salary was closed.", id);

        return employee;
    }

    public async Task<Employee> SendOnVacation(Guid id, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(id, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", id);
        
        if(employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException("Cannot send a fired employee on vacation.");
        
        if (employee.Status == EmployeeStatus.OnVacation)
            throw new BusinessRuleException($"Employee with ID '{id}' is already on vacation.");

        employee.Status = EmployeeStatus.OnVacation;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return employee;
    }

    public async Task<Employee> SendOffVacation(Guid id, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(id, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", id);
        
        if(employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException("Cannot process vacation status for a fired employee.");
        
        if (employee.Status != EmployeeStatus.OnVacation)
            throw new BusinessRuleException($"Employee with ID '{id}' is not currently on vacation.");

        employee.Status = EmployeeStatus.Active;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return employee;
    }
}