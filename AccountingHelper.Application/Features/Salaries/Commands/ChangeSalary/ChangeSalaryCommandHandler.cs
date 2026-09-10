using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Contracts;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Salaries.Commands.ChangeSalary;

public class ChangeSalaryCommandHandler : IRequestHandler<ChangeSalaryCommand, SalaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChangeSalaryCommandHandler> _logger;

    public ChangeSalaryCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ChangeSalaryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    public async Task<SalaryResponse> Handle(ChangeSalaryCommand request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, ct);
        
        if(employee == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        if (employee.Status == EmployeeStatus.Fired)
            throw new BusinessRuleException($"Cannot change salary of a fired employee with ID '{request.EmployeeId}'.");

        var currentSalary = await _unitOfWork.Salaries.GetCurrentSalaryAsync(request.EmployeeId, ct);
        
        var oldSalaryAmount = currentSalary?.Amount;
        var oldSalaryType = currentSalary?.Type;
        
        if (currentSalary != null)
            await _unitOfWork.Salaries.CloseAsync(currentSalary.Id, ct);

        var salary = new Salary
        {
            Id = Guid.NewGuid(),
            Type = request.SalaryType,
            Amount = request.Amount,
            EffectiveDate = DateTime.UtcNow,
            EmployeeId = employee.Id
        };
        
        _unitOfWork.Salaries.Add(salary);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new SalaryChanged(employee.Id, salary.Id),ct);
        
        _logger.LogInformation(
            "Salary updated for employee {EmployeeId}. Old: {OldAmount} ({OldType}), New: {NewAmount} ({NewType}).",
            employee.Id,
            oldSalaryAmount,
            oldSalaryType,
            salary.Amount,
            salary.Type);
        
        return salary.ToResponse();
    }
}