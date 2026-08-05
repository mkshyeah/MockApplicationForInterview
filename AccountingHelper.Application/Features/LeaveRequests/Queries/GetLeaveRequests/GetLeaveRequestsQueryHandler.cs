using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequests;

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, IReadOnlyList<LeaveRequest>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLeaveRequestsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;   
    }


    public async Task<IReadOnlyList<LeaveRequest>> Handle(GetLeaveRequestsQuery request, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(request.EmployeeId, ct);

        if (status == null)
            throw new NotFoundException("Employee", request.EmployeeId);

        var leaveRequests = await _unitOfWork.LeaveRequests
            .GetByEmployeeAsync(request.EmployeeId, ct);

        return leaveRequests;
    }
}