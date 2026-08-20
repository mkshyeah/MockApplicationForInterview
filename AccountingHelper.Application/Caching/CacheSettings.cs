namespace AccountingHelper.Application.Caching;

public class CacheSettings
{
    public const string SectionName = "Cache";
    
    public TimeSpan Dashboard { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan Lookups { get; init; } = TimeSpan.FromHours(1);
}