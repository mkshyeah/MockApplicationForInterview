using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class ImportJobRepository : IImportJobRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ImportJobRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        return entity?.ToModel();
    }

    public void Add(ImportJob job) => _dbContext.Add(job.ToEntity());

    public void AddFile(ImportFile model) => _dbContext.ImportFiles.Add(model.ToEntity());

    public async Task<ImportFile?> GetFileAsync(Guid jobId, CancellationToken ct)
    {
        var entity = await _dbContext.ImportFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ImportJobId == jobId, ct);

        return entity?.ToModel();
    }

    public async Task ApplyProgressAsync(ImportJob model, CancellationToken ct)
    {
        var entity = await _dbContext.ImportJobs
            .FirstOrDefaultAsync(x => x.Id == model.Id, ct);
        
        if (entity is null)
            throw new InvalidOperationException($"ImportJob '{model.Id}' was not found for update.");

        entity.Status = model.Status;
        entity.TotalRows = model.TotalRows;
        entity.ImportedCount = model.ImportedCount;
        entity.FailedCount = model.FailedCount;
        entity.StartedAt = model.StartedAt;
        entity.CompletedAt = model.CompletedAt;
    }

    public void AddErrors(IEnumerable<ImportError> models) => _dbContext.ImportErrors.AddRange(models.Select(e => e.ToEntity()));

    public async Task<(IReadOnlyList<ImportError> Items, int TotalCount)> GetErrorsAsync(Guid jobId, int offset, int limit, CancellationToken ct)
    {
        var query = _dbContext.ImportErrors
            .AsNoTracking()
            .Where(e => e.ImportJobId == jobId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.RowNumber)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
        
        return (items.Select(e => e.ToModel()).ToList(), total);
    }
}