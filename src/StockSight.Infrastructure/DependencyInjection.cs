using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure.Caching;
using StockSight.Infrastructure.Data;
using StockSight.Infrastructure.MarketData;

namespace StockSight.Infrastructure;

/// <summary>
/// Registers all Infrastructure services (PostgreSQL, Redis, market data) in DI.
/// Call from the API's Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- PostgreSQL via EF Core ---
        services.AddDbContext<StockSightDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        // --- Redis cache ---
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        var redisConn = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"]
                        ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // --- Market data ---
        services.AddSingleton<IStockDataProvider, YahooStockDataProvider>();

        return services;
    }
}
