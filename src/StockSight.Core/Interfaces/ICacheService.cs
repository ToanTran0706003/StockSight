namespace StockSight.Core.Interfaces;

/// <summary>
/// Abstraction over the distributed cache (Redis). Keeps Core free of any
/// StackExchange.Redis dependency so it can be referenced by the Blazor client.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);

    Task<bool> RemoveAsync(string key, CancellationToken ct = default);
}
