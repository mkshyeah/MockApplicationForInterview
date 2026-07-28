using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, GetEmployeesResult>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetEmployeesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetEmployeesResult> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        var (employees, total) = await _unitOfWork.Employees.GetFilteredAsync(
            request.Offset,
            request.Limit,
            request.OrderBy,
            request.Direction,
            request.DepartmentId,
            request.EmployeeStatus,
            ct);
        
        return new GetEmployeesResult(Items:employees, TotalCount:total);
    }
}