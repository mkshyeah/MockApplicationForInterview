using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

public class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, LeaveRequest>
{
    private readonly IUnitOfWork _unitOfWork;
    public RejectLeaveRequestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<LeaveRequest> Handle(RejectLeaveRequestCommand request, CancellationToken ct)
    {
        var leaveRequest = await _unitOfWork.LeaveRequests.GetByIdAsync(request.LeaveRequestId, ct);
        if (leaveRequest == null)
            throw new NotFoundException("Leave request", request.LeaveRequestId);

        if (leaveRequest.Status != LeaveStatus.Pending)
            throw new BusinessRuleException(
                $"Leave request '{leaveRequest.Id}' cannot be rejected because it is already {leaveRequest.Status}. " +
                "Only pending requests can be rejected.");

        leaveRequest.Status = LeaveStatus.Rejected;
        leaveRequest.DecidedAt = DateTime.UtcNow;
        await _unitOfWork.LeaveRequests.ApplyDecisionAsync(leaveRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return leaveRequest;
    }
}