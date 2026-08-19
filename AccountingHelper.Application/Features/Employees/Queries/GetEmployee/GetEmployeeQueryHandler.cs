using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployee;

public class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, EmployeeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetEmployeeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<EmployeeResponse> Handle(GetEmployeeQuery request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, ct);
        
        if (employee == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        return employee.ToResponse();
    }
}