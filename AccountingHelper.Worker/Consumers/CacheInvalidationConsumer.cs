using AccountingHelper.Application.Caching;
using AccountingHelper.Contracts.Messaging;
using MassTransit;
using Microsoft.Extensions.Caching.Hybrid;

namespace AccountingHelper.Worker.Consumers;

public sealed class CacheInvalidationConsumer :
    IConsumer<SalaryChanged>,
    IConsumer<EmployeeFired>,
    IConsumer<EmployeeHired>,
    IConsumer<ImportCompleted>
{
    private readonly HybridCache _cache;
    private readonly ILogger<CacheInvalidationConsumer> _logger;

    public CacheInvalidationConsumer(
        HybridCache cache,
        ILogger<CacheInvalidationConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SalaryChanged> context)
    {
        _logger.LogDebug(
            "Salary updated for employee {EmployeeId}. Dashboard cache was invalidated."
            ,context.Message.EmployeeId);
        return EvictDashboardAsync(context.CancellationToken);
    }

    public Task Consume(ConsumeContext<EmployeeFired> context)
    {
        _logger.LogDebug(
            "Employee {EmployeeId} was fired. Dashboard cache was invalidated."
            ,context.Message.EmployeeId);
        return EvictDashboardAsync(context.CancellationToken);
    }

    public Task Consume(ConsumeContext<EmployeeHired> context)
    {
        _logger.LogDebug(
            "Employee {EmployeeId} was hired. Dashboard cache was invalidated."
            ,context.Message.EmployeeId);
        return EvictDashboardAsync(context.CancellationToken);
    }
    
    // One eviction per import, not one per hired employee: the import publishes no EmployeeHired, so a
    // ten-thousand-row file costs the cache exactly what a single hire does.
    public Task Consume(ConsumeContext<ImportCompleted> context)
    {
        _logger.LogDebug(
            "Import job {ImportJobId} completed with {ImportedCount} employees. Dashboard cache was invalidated."
            ,context.Message.ImportJobId, context.Message.ImportedCount);
        
        return EvictDashboardAsync(context.CancellationToken);
    }
    
    private async Task EvictDashboardAsync(CancellationToken ct) =>
        await _cache.RemoveAsync(CacheKeys.ReportingDashboard, ct);
}