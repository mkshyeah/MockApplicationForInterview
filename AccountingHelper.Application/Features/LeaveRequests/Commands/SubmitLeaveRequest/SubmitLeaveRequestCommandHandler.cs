using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, LeaveRequest>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitLeaveRequestCommandHandler> _logger;

    public SubmitLeaveRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SubmitLeaveRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<LeaveRequest> Handle(SubmitLeaveRequestCommand request, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(request.EmployeeId, ct);

        if (status == null) 
            throw new NotFoundException("Employee", request.EmployeeId);
        
        if (status == EmployeeStatus.Fired)
            throw new BusinessRuleException($"Employee '{request.EmployeeId}' is fired and cannot submit leave requests.");


        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = LeaveStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        
        _unitOfWork.LeaveRequests.Add(leaveRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Leave request {LeaveRequestId} submitted by employee {EmployeeId} for {Days} days",
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            leaveRequest.DurationInDays);
                    
        
        return leaveRequest;
    }
}