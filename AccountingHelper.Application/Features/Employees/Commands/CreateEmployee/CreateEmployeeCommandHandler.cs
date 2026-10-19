using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Services;
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
        var email = EmailNormalizer.Normalize(request.Email);
        
        var exists = await _unitOfWork.Employees.ExistsByEmailAsync(email,ct);
        if (exists)
            throw new ConflictException($"Employee with email '{email}' already exists.");

        var department = await _unitOfWork.Departments.GetByIdAsync(request.DepartmentId, ct);
        if (department is null)
            throw new NotFoundException("Department", request.DepartmentId);

        var position = await _unitOfWork.Positions.GetByIdAsync(request.PositionId, ct);
        if (position is null)
            throw new NotFoundException("Position", request.PositionId);

        var (employee, balances) = EmployeeFactory.Hire(
            request.FirstName, request.LastName, email,
            request.DepartmentId, request.PositionId, request.HireDate,
            request.SalaryAmount, request.SalaryType);
        
        // Only the response needs these; the factory works with ids so the import does not load them per row.
        employee.Department = department;
        employee.Position = position;
        
        foreach (var balance in balances)
        {
            _unitOfWork.LeaveBalances.Add(balance);
        }
        
        _unitOfWork.Employees.Add(employee);

        try
        {
            await _publishEndpoint.Publish(new EmployeeHired(employee.Id), ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        // The check above is a fast path, not a guarantee: two concurrent requests pass it together,
        // because reading and inserting are not one operation. ix_employees_email is what actually
        // stops the second insert, and this turns its rejection into the same 409 rather than a 500.
        catch (UniqueConstraintViolationException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent employee creation for {Email} was rejected by the unique index.",
                email);

            throw new ConflictException($"Employee with email '{email}' already exists.");
        }

        _logger.LogInformation("Employee {EmployeeId} was created.", employee.Id);
        
        return employee.ToResponse();
    }
}