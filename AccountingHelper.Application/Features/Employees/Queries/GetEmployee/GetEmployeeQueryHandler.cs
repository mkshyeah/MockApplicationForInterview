using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployee;

public class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, Employee>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetEmployeeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Employee> Handle(GetEmployeeQuery request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        return employee;
    }
}