using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Services;

public class EmployeeService:IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IReadOnlyList<Employee>> GetEmployees(EmployeeFilteredRequest request , CancellationToken ct )
    {
        var employees = await _unitOfWork.Employees.GetFilteredAsync(
            request.Offset,
            request.Limit,
            request.OrderBy,
            request.Direction,
            request.DepartmentId,
            request.EmployeeStatus,
            ct);

        return employees.ToList().AsReadOnly();
    }

    public async Task<Employee> GetEmployee(Guid id, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(id, ct);

        if (employee == null)
            throw new NotFoundException($"Employee with id {id} not found");

        return employee;
    }

    public async Task<Employee> CreateEmployee(Employee employee, CancellationToken ct)
    {
        var exists = await _unitOfWork.Employees
            .ExistsByEmailAsync(employee.Email, ct);
        
        if (exists)
            throw new ConflictException($"Employee with email {employee.Email} already exists");
        
        var department = await _unitOfWork.Departments.GetByIdAsync(employee.DepartmentId, ct);
        if (department == null)
            throw new NotFoundException($"Department with id {employee.DepartmentId} not found");
        
        var position = await _unitOfWork.Positions.GetByIdAsync(employee.PositionId, ct);
        if (position is null)
            throw new NotFoundException($"Position with id {employee.PositionId} not found");

        
        
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
            throw new NotFoundException($"Employee with id {id} not found");

        if (employee.Status == EmployeeStatus.Fired)
            throw new ConflictException($"Employee with id {id} is already fired");
        
        var currentSalary = await _unitOfWork.Salaries.GetCurrentSalaryAsync(id, ct);
        if (currentSalary != null)
            await _unitOfWork.Salaries.CloseAsync(currentSalary.Id, ct);
            
        employee.TerminationDate = DateTime.UtcNow;
        employee.Status = EmployeeStatus.Fired;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);

        return employee;
    }

    public async Task<int> CountEmployees(EmployeeFilteredRequest request, CancellationToken ct)
    {
        return await _unitOfWork.Employees
            .GetFilteredCountAsync(request.DepartmentId, request.EmployeeStatus, ct);
    }
}