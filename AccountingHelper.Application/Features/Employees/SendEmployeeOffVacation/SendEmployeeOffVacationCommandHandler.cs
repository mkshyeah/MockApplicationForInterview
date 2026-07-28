using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.SendEmployeeOffVacation;

public class SendEmployeeOffVacationCommandHandler : IRequestHandler<SendEmployeeOffVacationCommand, Employee>
{
    private readonly IUnitOfWork _unitOfWork;

    public SendEmployeeOffVacationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    public async Task<Employee> Handle(SendEmployeeOffVacationCommand request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees
            .GetByIdAsync(request.EmployeeId, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        if(employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException("Cannot process vacation status for a fired employee.");
        
        if (employee.Status != EmployeeStatus.OnVacation)
            throw new BusinessRuleException($"Employee with ID '{request.EmployeeId}' is not currently on vacation.");

        employee.Status = EmployeeStatus.Active;
        
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return employee;
    }
}