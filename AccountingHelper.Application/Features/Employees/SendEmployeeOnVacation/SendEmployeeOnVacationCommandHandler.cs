using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Employees.SendEmployeeOnVacation;

public class SendEmployeeOnVacationCommandHandler : IRequestHandler<SendEmployeeOnVacationCommand, Employee>
{
    private readonly IUnitOfWork _unitOfWork;

    public SendEmployeeOnVacationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Employee> Handle(SendEmployeeOnVacationCommand request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId,ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        if(employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException("Cannot send a fired employee on vacation.");
        
        if (employee.Status == EmployeeStatus.OnVacation)
            throw new BusinessRuleException($"Employee with ID '{request.EmployeeId}' is already on vacation.");


        employee.Status = EmployeeStatus.OnVacation;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return employee;
    }
}