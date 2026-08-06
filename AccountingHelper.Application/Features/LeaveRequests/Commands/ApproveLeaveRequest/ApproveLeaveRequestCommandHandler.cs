using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, LeaveRequest>
{
    private readonly IUnitOfWork _unitOfWork;
    public ApproveLeaveRequestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<LeaveRequest> Handle(ApproveLeaveRequestCommand request, CancellationToken ct)
    {
        var leaveRequest = await _unitOfWork.LeaveRequests.GetByIdAsync(request.LeaveRequestId, ct);
        if (leaveRequest == null)
            throw new NotFoundException("LeaveRequest", request.LeaveRequestId);
        
        if (leaveRequest.Status != LeaveStatus.Pending)
            throw new BusinessRuleException(
                $"Leave request '{leaveRequest.Id}' cannot be approved because it is already {leaveRequest.Status}. " +
                "Only pending requests can be approved.");

        var status = await _unitOfWork.Employees.GetStatusAsync(leaveRequest.EmployeeId, ct);

        if (status == EmployeeStatus.Fired)
            throw new BusinessRuleException( $"Employee '{leaveRequest.EmployeeId}' is fired, their leave requests cannot be approved.");

        var balance = await _unitOfWork.LeaveBalances
            .GetAsync(leaveRequest.EmployeeId, leaveRequest.LeaveType, ct);

        if (balance == null)
            throw new BusinessRuleException(
                $"No {leaveRequest.LeaveType} leave balance found for employee '{leaveRequest.EmployeeId}'.");

        if (balance.RemainingDays < leaveRequest.DurationInDays)
            throw new BusinessRuleException(
                $"Employee '{leaveRequest.EmployeeId}' has only {balance.RemainingDays} days of " +
                $"{leaveRequest.LeaveType} leave remaining, but the request covers {leaveRequest.DurationInDays}.");

        leaveRequest.Status = LeaveStatus.Approved;
        leaveRequest.DecidedAt = DateTime.UtcNow;
        
        await _unitOfWork.LeaveRequests.ApplyDecisionAsync(leaveRequest, ct);
        await _unitOfWork.LeaveBalances.DebitAsync(
            leaveRequest.EmployeeId, leaveRequest.LeaveType, leaveRequest.DurationInDays, ct);
        
        await _unitOfWork.SaveChangesAsync(ct);
        return leaveRequest;
    }
}