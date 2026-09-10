using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Application.Caching;

public sealed class CachePolicies
{
    public HybridCacheEntryOptions ReportingDashboard { get; }
    public HybridCacheEntryOptions Lookups { get; }

    public CachePolicies(IOptions<CacheSettings> settings)
    {
        ReportingDashboard = new HybridCacheEntryOptions
        {
            Expiration = settings.Value.Dashboard,
            LocalCacheExpiration = settings.Value.Dashboard,
            Flags = HybridCacheEntryFlags.DisableLocalCache
        };

        Lookups = new HybridCacheEntryOptions
        {
            Expiration = settings.Value.Lookups,
            LocalCacheExpiration = settings.Value.Lookups,
        };
    }
}