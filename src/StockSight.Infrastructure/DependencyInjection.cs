using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure.Alerts;
using StockSight.Infrastructure.AI;
using StockSight.Infrastructure.Caching;
using StockSight.Infrastructure.Data;
using StockSight.Infrastructure.MarketData;
using StockSight.Infrastructure.Portfolios;
using StockSight.Infrastructure.Watchlists;

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
        {
            if (bool.TryParse(configuration["Data:UseInMemory"], out var useInMemory) && useInMemory)
                options.UseInMemoryDatabase("StockSight");
            else
                options.UseNpgsql(configuration.GetConnectionString("Postgres"));
        });

        // --- Redis cache ---
        var redisSection = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(options =>
        {
            options.ConnectionString = redisSection["ConnectionString"] ?? options.ConnectionString;
            options.InstanceName = redisSection["InstanceName"] ?? options.InstanceName;
            if (TimeSpan.TryParse(redisSection["DefaultExpiry"], out var expiry))
                options.DefaultExpiry = expiry;
        });

        if (bool.TryParse(configuration["Cache:UseInMemory"], out var useInMemoryCache) && useInMemoryCache)
        {
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }
        else
        {
            var redisConn = redisSection["ConnectionString"] ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        // --- Market data ---
        services.AddSingleton<YahooStockDataProvider>();
        services.AddSingleton<MockStockDataProvider>();
        if (bool.TryParse(configuration["MarketData:UseMock"], out var useMockMarketData) && useMockMarketData)
            services.AddSingleton<IStockDataProvider, MockStockDataProvider>();
        else
            services.AddSingleton<IStockDataProvider, ResilientStockDataProvider>();
        services.AddSingleton<INewsService, NewsSentimentAnalyzer>();
        services.AddSingleton<ISignalEngine, SignalEngine>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<INewsFeedService, NewsFeedService>();

        return services;
    }
}
