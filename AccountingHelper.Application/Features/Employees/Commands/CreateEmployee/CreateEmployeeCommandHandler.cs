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

namespace AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IUnitOfWork unitOfWork, 
        IPublishEndpoint publishEndpoint,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<EmployeeResponse> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var exists = await _unitOfWork.Employees.ExistsByEmailAsync(request.Email,ct);
        if (exists)
            throw new ConflictException($"Employee with email '{request.Email}' already exists.");

        var department = await _unitOfWork.Departments.GetByIdAsync(request.DepartmentId, ct);
        if (department is null)
            throw new NotFoundException("Department", request.DepartmentId);

        var position = await _unitOfWork.Positions.GetByIdAsync(request.PositionId, ct);
        if (position is null)
            throw new NotFoundException("Position", request.PositionId);

        var employeeId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            Status = EmployeeStatus.Active,
            HireDate = request.HireDate,
            Department = department,
            Position = position,
            Salaries =
            {
                new Salary
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    Amount = request.SalaryAmount,
                    Type = request.SalaryType,
                    EffectiveDate = request.HireDate
                }
            }
        };
        
        foreach (var type in Enum.GetValues<LeaveType>())
        {
            _unitOfWork.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveType = type,
                RemainingDays = LeaveEntitlement.DaysFor(type)
            });
        }
        
        _unitOfWork.Employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new EmployeeHired(employee.Id), ct);
        
        _logger.LogInformation("Employee {EmployeeId} was created.", employeeId);

        return employee.ToResponse();
    }
}