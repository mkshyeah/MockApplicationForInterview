using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequest;

public class GetLeaveRequestQueryHandler : IRequestHandler<GetLeaveRequestQuery, LeaveRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLeaveRequestQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<LeaveRequestResponse> Handle(GetLeaveRequestQuery request, CancellationToken ct)
    {
        
        var leaveRequest = await _unitOfWork.LeaveRequests.GetByIdAsync(request.Id, ct);

        if (leaveRequest == null)
            throw new NotFoundException("LeaveRequest", request.Id);
        
        return leaveRequest.ToResponse();
    }
}

