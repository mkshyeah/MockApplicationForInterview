using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Application.Features.Imports.Parsing;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Domain.Services;
using FluentValidation;
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
    private readonly IValidator<CreateEmployeeCommand> _validator;
    private readonly ILogger<RunImportCommandHandler> _logger;

    public RunImportCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        IOptions<ImportSettings> importSettings,
        IValidator<CreateEmployeeCommand> validator,
        ILogger<RunImportCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _importSettings = importSettings.Value;
        _validator = validator;
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
        
        // Normalized email -> the line it first appeared on, across the whole file. Only committed batches
        // are merged in, so a batch that has to be staged again does not find its own emails here.
        var seenEmails = new Dictionary<string, int>(StringComparer.Ordinal);
        
        foreach (var batch in EmployeeCsvReader.Read(file.Content, skip: 0).Chunk(_importSettings.BatchSize))
        {
            var batchEmails = await StageBatchAsync(job, batch, references, seenEmails, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _unitOfWork.ClearTracking();

            foreach (var (email, rowNumber) in batchEmails)
            {
                seenEmails[email] = rowNumber;
            }
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
    // Returns the emails this batch claimed, for the caller to merge once the batch is committed.
    private async Task<Dictionary<string, int>> StageBatchAsync(
        ImportJob job,
        IReadOnlyList<ParsedCsvRow> batch,
        References references,
        IReadOnlyDictionary<string, int> seenEmails,
        CancellationToken ct)
    {
        var errors = new List<ImportError>();
        var batchEmails = new Dictionary<string, int>(StringComparer.Ordinal);
        var candidates = new List<(int RowNumber, string Email, CreateEmployeeCommand Command)>();
        

        foreach (var parsed in batch)
        {
            var mapping = EmployeeCsvRowMapper.Map(parsed.Row, references.Departments, references.Positions);

            if (mapping.Command is null)
            {
                errors.Add(ToError(job.Id, parsed.RowNumber, mapping.Errors));
                continue;
            }
            
            // The same rules the API applies, so the import cannot accept what POST /v1/employees rejects.
            var validation = _validator.Validate(mapping.Command);

            if (!validation.IsValid)
            {
                errors.Add(ToError(job.Id, parsed.RowNumber, validation.Errors.Select(e =>e.ErrorMessage)));
                continue;
            }
            
            // The one definition of "the same person", shared with the create handler and the unique index.
            var email = EmailNormalizer.Normalize(mapping.Command.Email);

            if (seenEmails.TryGetValue(email, out var firstRow) || batchEmails.TryGetValue(email, out firstRow))
            {
                errors.Add(ToError(job.Id, parsed.RowNumber,
                    [$"Email '{email}' duplicates row {firstRow}."]));
                continue;
            }
            
            batchEmails[email] = parsed.RowNumber;
            candidates.Add((parsed.RowNumber, email, mapping.Command));
        }
        
        // One query per batch instead of ExistsByEmailAsync per row.
        var existing = await _unitOfWork.Employees.GetExistingEmailsAsync(candidates.Select(c => c.Email).ToList(), ct);
        var imported = 0;

        foreach (var (rowNumber, email, command) in candidates)
        {
            if (existing.Contains(email))
            {
                errors.Add(ToError(job.Id, rowNumber, [$"Employee with email '{email}' already exists."]));
                continue;
            }

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
        
        return batchEmails;
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
