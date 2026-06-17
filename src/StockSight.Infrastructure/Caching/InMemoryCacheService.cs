using System.Collections.Concurrent;
using StockSight.Core.Interfaces;

namespace StockSight.Infrastructure.Caching;

public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _items = new();
    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _subscriptions = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_items.TryGetValue(key, out var entry))
            return Task.FromResult<T?>(default);

        if (entry.ExpiresUtc is not null && entry.ExpiresUtc <= DateTime.UtcNow)
        {
            _items.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(entry.Value is T typed ? typed : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        _items[key] = new CacheEntry(value, expiry is null ? null : DateTime.UtcNow.Add(expiry.Value));
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_items.TryRemove(key, out _));

    public async Task PublishAsync<T>(string channel, T value, CancellationToken ct = default)
    {
        if (!_subscriptions.TryGetValue(channel, out var handlers))
            return;

        foreach (var handler in handlers.ToArray())
            await handler(value!);
    }

    public Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken ct = default)
    {
        var handlers = _subscriptions.GetOrAdd(channel, _ => new List<Func<object, Task>>());
        lock (handlers)
        {
            handlers.Add(value => value is T typed ? handler(typed) : Task.CompletedTask);
        }

        return Task.CompletedTask;
    }

    private sealed record CacheEntry(object? Value, DateTime? ExpiresUtc);
}
