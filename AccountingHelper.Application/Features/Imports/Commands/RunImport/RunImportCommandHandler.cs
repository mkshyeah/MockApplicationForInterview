using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Application.Features.Imports.Parsing;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
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
    private const int HeaderRowNumber = 1;
    // A lost email race is resolved by restaging, and losing it twice on the same batch is already unlikely.
    // Past this the exception goes to the retry policy rather than looping here.
    private const int MaxBatchAttempts = 3;
    
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

        var file = await _unitOfWork.ImportJobs.GetFileAsync(job.Id, ct);

        if (file == null)
        {
            await FailAsync(job, "The uploaded file is missing.", ct);
            return;
        }

        try
        {
            job.TotalRows = EmployeeCsvReader.CountRecords(file.Content);
        }
        catch (InvalidImportFileException ex)
        {
            await FailAsync(job, ex.Message, ct);
            return;
        }

        job.Status = ImportJobStatus.Processing;
        job.StartedAt ??= DateTime.UtcNow;

        await _unitOfWork.ImportJobs.ApplyProgressAsync(job, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var references = await LoadReferencesAsync(ct);

        // The checkpoint. Counters are committed in the same SaveChangesAsync as their batch and records are
        // handled in order, so after any crash Imported + Failed is exactly the number of records already done.
        // A redelivery or a retry resumes there instead of reporting its own earlier work as "already exists".
        var processed = job.ImportedCount + job.FailedCount;

        if (processed > 0)
        {
            _logger.LogInformation(
                "Import job {ImportJobId} resumes after {Processed} of {TotalRows} records.",
                job.Id, processed, job.TotalRows);
        }
        
        // Normalized email -> the line it first appeared on, across the whole file. Only committed batches
        // are merged in, so a batch that has to be staged again does not find its own emails here.
        var seenEmails = new Dictionary<string, int>(StringComparer.Ordinal);
        
        foreach (var batch in EmployeeCsvReader.Read(file.Content, skip: processed).Chunk(_importSettings.BatchSize))
        {
            var batchEmails = await CommitBatchAsync(job, batch, references, seenEmails, ct);

            if (batchEmails is null)
                return;

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
    
    // Stages and saves one batch, resolving unique violations by the index that raised them.
    // Returns null when another delivery is processing the same job and this one has to stop.
    private async Task<Dictionary<string, int>?> CommitBatchAsync(
        ImportJob job,
        IReadOnlyList<ParsedCsvRow> batch,
        References references,
        IReadOnlyDictionary<string, int> seenEmails,
        CancellationToken ct)
    {
        var importedBefore = job.ImportedCount;
        var failedBefore = job.FailedCount;

        for (var attempt = 1;; attempt++)
        {
            var batchEmails = await StageBatchAsync(job, batch, references, seenEmails, ct);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                _unitOfWork.ClearTracking();
                return batchEmails;
            }
            // Someone hired one of these people between our lookup and our insert. Postgres reports a unique
            // violation only once the competing insert has committed, so restaging runs GetExistingEmailsAsync
            // again and sees the winner. The clear comes first: the rolled-back rows are still tracked as Added
            // and would otherwise ride along with the restaged batch.
            catch (UniqueConstraintViolationException ex)
                when (ex.ConstraintName == DatabaseConstraints.EmployeeEmail && attempt < MaxBatchAttempts)
            {
                _logger.LogInformation(
                    "Import job {ImportJobId} lost an email race; restaging the batch (attempt {Attempt}).",
                    job.Id, attempt);

                _unitOfWork.ClearTracking();
                job.ImportedCount = importedBefore;
                job.FailedCount = failedBefore;
            }
            // Another delivery has already written an error row for this line, so it owns the job. Stop and let
            // the message be acknowledged, without touching the status: that delivery is still working, and
            // marking the job completed from here would publish counters that are not final.
            catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == DatabaseConstraints.ImportErrorRow)
            {
                _logger.LogInformation(ex, "Import job {ImportJobId} is being processed by another delivery.", job.Id);
                return null;
            }
        }
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
    
    // The file cannot be read as a whole. Deterministic, so the handler returns instead of throwing: five
    // retries would only repeat the same answer. The reason goes into the existing report as one error on
    // the header line, so the client reads it where it already reads row errors.
    private async Task FailAsync(ImportJob job, string reason, CancellationToken ct)
    {
        job.Status = ImportJobStatus.Failed;
        job.StartedAt ??= DateTime.UtcNow;
        job.CompletedAt = DateTime.UtcNow;
        
        _unitOfWork.ImportJobs.AddErrors([ToError(job.Id, HeaderRowNumber, [reason])]);
        await _unitOfWork.ImportJobs.ApplyProgressAsync(job, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        // A concurrent delivery reached the same verdict on the same file and wrote it first.
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == DatabaseConstraints.ImportErrorRow)
        {
            _logger.LogInformation(ex, "Import job {ImportJobId} was already failed by another delivery.", job.Id);
            return;
        }
        
        _logger.LogWarning("Import job {ImportJobId} failed: {Reason}", job.Id, reason);
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
