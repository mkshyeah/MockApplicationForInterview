using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Imports.Parsing;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Domain.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Application.Features.Imports.Commands.RunImport;

public class RunImportCommandHandler : IRequestHandler<RunImportCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ImportSettings _importSettings;
    private readonly ILogger<RunImportCommandHandler> _logger;

    public RunImportCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        IOptions<ImportSettings> importSettings,
        ILogger<RunImportCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _importSettings = importSettings.Value;
        _logger = logger;
    }
    
    public async Task Handle(RunImportCommand request, CancellationToken ct)
    {
        var job = await _unitOfWork.ImportJobs.GetByIdAsync(request.ImportJobId, ct);

        if (job == null)
        {
            throw new NotFoundException("ImportJob", request.ImportJobId);
        }
        
        // Fast path for a redelivered message: a finished job must not be walked again or publish twice.
        if (job.Status is ImportJobStatus.Completed or ImportJobStatus.Failed)
        {
            _logger.LogInformation("Import job {ImportJobId} is already {Status}; ignoring redelivery.", job.Id, job.Status);
            return;
        }
        
        var file = await _unitOfWork.ImportJobs.GetFileAsync(job.Id, ct)
            ?? throw new InvalidOperationException($"Import job '{job.Id}' has no file.");

        job.TotalRows = EmployeeCsvReader.CountRecords(file.Content);
        job.Status = ImportJobStatus.Processing;
        job.StartedAt ??= DateTime.UtcNow;

        await _unitOfWork.ImportJobs.ApplyProgressAsync(job, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var references = await LoadReferencesAsync(ct);

        foreach (var batch in EmployeeCsvReader.Read(file.Content, skip: 0).Chunk(_importSettings.BatchSize))
        {
            await StageBatchAsync(job, batch, references, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _unitOfWork.ClearTracking();
        }
        
        job.Status = ImportJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        
        await _unitOfWork.ImportJobs.ApplyProgressAsync(job, ct);
        // After the last ClearTracking: the outbox row lives in the change tracker until the save.
        await _publishEndpoint.Publish(new ImportCompleted(job.Id, job.ImportedCount, job.FailedCount), ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Import job {ImportJobId} completed: {ImportedCount} imported, {FailedCount} failed of {TotalRows}.",
            job.Id, job.ImportedCount, job.FailedCount, job.TotalRows);
    }
    
    // Everything one batch writes — employees, balances, error rows, progress — is staged here and lands in
    // a single SaveChangesAsync, so the counters can never describe rows that were not committed.
    private async Task StageBatchAsync(
        ImportJob job,
        IReadOnlyList<ParsedCsvRow> batch,
        References references,
        CancellationToken ct)
    {
        var errors = new List<ImportError>();
        var imported = 0;

        foreach (var parsed in batch)
        {
            var mapping = EmployeeCsvRowMapper.Map(parsed.Row, references.Departments, references.Positions);

            if (mapping.Command is null)
            {
                errors.Add(ToError(job.Id, parsed.RowNumber, mapping.Errors));
                continue;
            }
            
            var command = mapping.Command;
            var (employee, balances) = EmployeeFactory.Hire(
                command.FirstName, command.LastName, command.Email,
                command.DepartmentId, command.PositionId, command.HireDate,
                command.SalaryAmount, command.SalaryType);
            
            _unitOfWork.Employees.Add(employee);
            foreach (var balance in balances)
            {
                _unitOfWork.LeaveBalances.Add(balance);
            }
            
            imported++;
        }
        
        _unitOfWork.ImportJobs.AddErrors(errors);
        
        job.ImportedCount += imported;
        job.FailedCount += errors.Count;
        
        await _unitOfWork.ImportJobs.ApplyProgressAsync(job, ct);
    }
    
    // One error row per file row, never one per broken rule: it keeps the report's length equal to FailedCount.
    private static ImportError ToError(Guid jobId, int rowNumber, IEnumerable<string> messages)
    {
        var message = string.Join(" ", messages);

        return new ImportError
        {
            Id = Guid.NewGuid(),
            ImportJobId = jobId,
            RowNumber = rowNumber,
            Message = message.Length <= ImportError.MessageMaxLength
                ? message
                : message[..(ImportError.MessageMaxLength - 1)] + "…"
        };
    }

    // Loaded once per import: reference lists are small, and a lookup per row would be N queries.
    private async Task<References> LoadReferencesAsync(CancellationToken ct)
    {
        var departments = await _unitOfWork.Departments.GetAllAsync(ct);
        var positions = await _unitOfWork.Positions.GetAllAsync(ct);

        return new References(
            departments.ToDictionary(d => d.Name, d => d.Id, StringComparer.Ordinal),
            positions.ToDictionary(d => d.Title, d => d.Id, StringComparer.Ordinal));
    }
    
    private sealed record References(
        IReadOnlyDictionary<string, Guid> Departments,
        IReadOnlyDictionary<string, Guid> Positions);
}
