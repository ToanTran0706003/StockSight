using Microsoft.Extensions.Options;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.BackgroundServices;

public class StockDataIngestionService : BackgroundService
{
    private readonly IStockDataProvider _provider;
    private readonly ICacheService _cache;
    private readonly IStockBroadcaster _broadcaster;
    private readonly IOptionsMonitor<StockIngestionOptions> _options;
    private readonly ILogger<StockDataIngestionService> _logger;

    public StockDataIngestionService(
        IStockDataProvider provider,
        ICacheService cache,
        IStockBroadcaster broadcaster,
        IOptionsMonitor<StockIngestionOptions> options,
        ILogger<StockDataIngestionService> logger)
    {
        _provider = provider;
        _cache = cache;
        _broadcaster = broadcaster;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
            {
                await Task.Delay(options.PollInterval, stoppingToken);
                continue;
            }

            try
            {
                var symbols = options.Symbols
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Where(s => s.Length > 0)
                    .Distinct()
                    .ToArray();

                if (symbols.Length > 0)
                    await PollAndBroadcastAsync(symbols, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stock ingestion loop failed.");
            }

            await Task.Delay(options.PollInterval, stoppingToken);
        }
    }

    private async Task PollAndBroadcastAsync(string[] symbols, CancellationToken ct)
    {
        IReadOnlyList<StockTick> ticks = await _provider.GetQuotesAsync(symbols, ct);
        foreach (var tick in ticks)
        {
            string cacheKey = $"quote:{tick.Symbol}";
            string channel = $"tick:{tick.Symbol}";

            await _cache.SetAsync(cacheKey, tick, TimeSpan.FromSeconds(30), ct);
            await _cache.PublishAsync(channel, tick, ct);
            await _broadcaster.BroadcastTickAsync(tick, ct);
        }
    }
}
