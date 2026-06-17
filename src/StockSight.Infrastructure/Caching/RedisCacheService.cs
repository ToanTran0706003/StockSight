using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StockSight.Core.Interfaces;

namespace StockSight.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> implementation over StackExchange.Redis with JSON serialization.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    private IDatabase Db => _redis.GetDatabase();

    private string Prefixed(string key) => $"{_options.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        RedisValue value = await Db.StringGetAsync(Prefixed(key));
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        await Db.StringSetAsync(Prefixed(key), json, expiry ?? _options.DefaultExpiry);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
        => Db.KeyDeleteAsync(Prefixed(key));
}
