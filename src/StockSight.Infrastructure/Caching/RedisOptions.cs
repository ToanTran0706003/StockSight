namespace StockSight.Infrastructure.Caching;

/// <summary>Bound from the "Redis" configuration section.</summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis connection string, e.g. "localhost:6379".</summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>Optional key prefix applied to every cache entry.</summary>
    public string InstanceName { get; set; } = "stocksight:";

    /// <summary>Default time-to-live for cached quotes.</summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromSeconds(30);
}
