using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Employees.CreateEmployee;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Employee>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IUnitOfWork unitOfWork, 
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Employee> Handle(CreateEmployeeCommand request, CancellationToken ct)
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
        
        _unitOfWork.Employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(ct);
        
        _logger.LogInformation("Employee {EmployeeId} was created.", employeeId);

        return (await _unitOfWork.Employees.GetByIdAsync(employeeId, ct))!;
    }
}