using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IImportJobRepository
{
    Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(ImportJob job);
    void AddFile(ImportFile model);
    Task<ImportFile?> GetFileAsync(Guid jobId, CancellationToken ct);
    Task ApplyProgressAsync(ImportJob model, CancellationToken ct);
    void AddErrors(IEnumerable<ImportError> models);
    Task<(IReadOnlyList<ImportError> Items, int TotalCount)> GetErrorsAsync(
        Guid jobId, int offset, int limit, CancellationToken ct); 
    
}