using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetEmployeeStatus;

public class GetEmployeeStatusQueryHandler : IRequestHandler<GetEmployeeStatusQuery, EmployeeStatus>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetEmployeeStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<EmployeeStatus> Handle(GetEmployeeStatusQuery request, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees
            .GetStatusAsync(request.EmployeeId, ct);
        
        if (!status.HasValue)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        return status.Value;
    }
}