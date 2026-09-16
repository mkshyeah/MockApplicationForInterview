using AccountingHelper.Application.Exceptions;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Domain.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Payroll.Commands.RunPayroll;

public class RunPayrollCommandHandler : IRequestHandler<RunPayrollCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RunPayrollCommandHandler> _logger;

    public RunPayrollCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<RunPayrollCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Handle(RunPayrollCommand request, CancellationToken ct)
    {
        var run = await _unitOfWork.PayrollRuns
            .GetByIdAsync(request.PayrollRunId, ct);

        if (run == null)
            throw new NotFoundException("PayrollRun", request.PayrollRunId);

        // Fast path for a redelivered message, not a guarantee: two deliveries can pass this at                                                                                                             
        // the same time. The unique index on (payroll_run_id, employee_id) is what protects money.
        if (run.Status == PayrollRunStatus.Completed)
        {
            _logger.LogInformation("Payroll run {PayrollRunId} is already completed; ignoring redelivery.", run.Id);
            return;
        }
        
        run.Status = PayrollRunStatus.Processing;
        run.StartedAt = DateTime.UtcNow;
        
        await _unitOfWork.PayrollRuns.ApplyProgressAsync(run, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        
        // The whole company lands in one list. Fine at a few thousand employees; past roughly ten                                                                                                           
        // thousand this wants paging, and the symptom will be memory, not slowness.                                                                                                                         
        var snapshots = await _unitOfWork.Employees.GetPayrollEmployeeSnapshotsAsync(ct);

        var payslips = new List<Payslip>();
        var skipped = 0;

        foreach (var snapshot in snapshots)
        {
            if (snapshot.Amount is null || snapshot.SalaryType is null)
            {
                // A missing active salary is bad data, not a reason to fail the batch: one broken
                // row must not cost everyone else their payslip.
                skipped++;
                continue;
            }

            var amount = snapshot.Amount.Value;
            var type  = snapshot.SalaryType.Value;

            var gross = SalaryCalculator.Convert(amount, type, SalaryType.Monthly);
            var tax = SalaryCalculator.CalculateTaxes(amount, type);
            
            // Placeholder for this phase: nothing produces deductions yet, and Net keeps the full
            // formula so it stays correct once something does.                                                                                                                                              
            const decimal deductions = 0m;
            
            payslips.Add(new Payslip
            {
                Id = Guid.NewGuid(),
                PayrollRunId = run.Id,
                EmployeeId = snapshot.EmployeeId,
                Gross = Round(gross),
                Tax = Round(tax),
                Deductions = Round(deductions),
                Net = Round(gross-tax-deductions)
            });
        }
        
        _unitOfWork.Payslips.AddRange(payslips);
        
        run.Status = PayrollRunStatus.Completed;
        run.ProcessedCount = payslips.Count;
        run.SkippedCount = skipped;
        run.CompletedAt = DateTime.UtcNow;
        
        await _unitOfWork.PayrollRuns.ApplyProgressAsync(run, ct);

        try
        {
            await _publishEndpoint.Publish(
                new PayrollRunCompleted(run.Id, run.ProcessedCount, run.SkippedCount), ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException ex)
        {
            // A concurrent delivery got there first, so the work is done. Acknowledge the message:                                                                                                          
            // throwing would retry and eventually dead-letter a run that actually succeeded.                                                                                                                
            _logger.LogInformation(ex, "Payroll run {PayrollRunId} was already processed concurrently.", run.Id);                                                                                            
            return;
        }
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}