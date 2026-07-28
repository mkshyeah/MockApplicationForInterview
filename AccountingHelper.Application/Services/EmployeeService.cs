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
}