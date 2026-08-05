using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveBalances.Queries.GetLeaveBalances;

public class GetLeaveBalancesQueryHandler : IRequestHandler<GetLeaveBalancesQuery, IReadOnlyList<LeaveBalance>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLeaveBalancesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    
    public async Task<IReadOnlyList<LeaveBalance>> Handle(GetLeaveBalancesQuery request, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees
            .GetStatusAsync(request.EmployeeId, ct);
        
        if(status==null)
            throw new NotFoundException("Employee", request.EmployeeId);

        var leaveBalances = await _unitOfWork.LeaveBalances
            .GetForEmployeeAsync(request.EmployeeId, ct);
        
        return leaveBalances;
    }
}