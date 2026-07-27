using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Employees.FireEmployee;

public class FireEmployeeCommandHandler : IRequestHandler<FireEmployeeCommand, Employee>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FireEmployeeCommandHandler> _logger;

    public FireEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<FireEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Employee> Handle(FireEmployeeCommand request, CancellationToken ct)
    {
        var employee= await _unitOfWork.Employees
            .GetByIdAsync(request.Id, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", request.Id);
        
        if (employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException($"Employee with ID '{request.Id}' is already fired.");
        
        var currentSalary = await _unitOfWork.Salaries.GetCurrentSalaryAsync(request.Id, ct);
        if (currentSalary != null)
            await _unitOfWork.Salaries.CloseAsync(currentSalary.Id, ct);
            
        employee.TerminationDate = DateTime.UtcNow;
        employee.Status = EmployeeStatus.Fired;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        _logger.LogInformation("Employee {EmployeeId} was successfully fired. Associated active salary was closed.", request.Id);

        return employee;
    }
}