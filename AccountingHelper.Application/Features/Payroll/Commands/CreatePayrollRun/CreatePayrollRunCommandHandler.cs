using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Payroll.Commands.CreatePayrollRun;

public class CreatePayrollRunCommandHandler : IRequestHandler<CreatePayrollRunCommand, PayrollRunResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreatePayrollRunCommandHandler> _logger;

    public CreatePayrollRunCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CreatePayrollRunCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<PayrollRunResponse> Handle(CreatePayrollRunCommand request, CancellationToken ct)
    {
        if (await _unitOfWork.PayrollRuns.ExistsForPeriodAsync(request.Year, request.Month, ct))
            throw new ConflictException($"Payroll run for {request.Year}-{request.Month:00} already exists.");

        var payrollRun = new PayrollRun
        {
            Id = Guid.NewGuid(),
            Year = request.Year,
            Month = request.Month,
            Status = PayrollRunStatus.Pending
        };
        
        _unitOfWork.PayrollRuns.Add(payrollRun);

        try
        {
            await _publishEndpoint.Publish(new PayrollRunRequested(payrollRun.Id, payrollRun.Year, payrollRun.Month), ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        // The check above is a courtesy, not a guarantee: two concurrent requests both pass it,
        // because reading and inserting are not one operation. The unique index is what actually
        // stops the second run, and this turns its rejection into the same 409 rather than a 500.
        catch (UniqueConstraintViolationException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent payroll run creation for {Year}-{Month:00} was rejected by the unique index.",
                request.Year,
                request.Month);

            throw new ConflictException($"Payroll run for {request.Year}-{request.Month:00} already exists.");
        }

        return payrollRun.ToResponse();
    }
}