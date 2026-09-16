using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class PayrollRunRepository : IPayrollRunRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PayrollRunRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.PayrollRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(pr => pr.Id == id, ct);

        return entity?.ToModel();
    }

    public async Task<bool> ExistsForPeriodAsync(int year, int month, CancellationToken ct)
    {
        return await _dbContext.PayrollRuns
            .AnyAsync(pr => pr.Year == year && pr.Month == month, ct);
    }

    public void Add(PayrollRun payrollRun) => _dbContext.Add(payrollRun.ToEntity());
    
    public async Task ApplyProgressAsync(PayrollRun model, CancellationToken ct)
    {
        var entity = await _dbContext.PayrollRuns.
            FirstOrDefaultAsync(x => x.Id == model.Id, ct);

        if (entity is null)
            throw new InvalidOperationException($"Payroll run '{model.Id}' was not found for update.");
        
        entity.Status = model.Status;
        entity.ProcessedCount = model.ProcessedCount;
        entity.SkippedCount = model.SkippedCount;
        entity.StartedAt = model.StartedAt;
        entity.CompletedAt = model.CompletedAt;
    }
}