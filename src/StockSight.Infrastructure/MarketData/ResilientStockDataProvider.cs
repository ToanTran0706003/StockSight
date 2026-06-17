using Polly;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.Infrastructure.MarketData;

public class ResilientStockDataProvider : IStockDataProvider
{
    private readonly IStockDataProvider _primary;
    private readonly IStockDataProvider _fallback;
    private readonly ResiliencePipeline _pipeline;

    public ResilientStockDataProvider(YahooStockDataProvider primary, MockStockDataProvider fallback)
    {
        _primary = primary;
        _fallback = fallback;
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
    }

    public async Task<StockTick?> GetQuoteAsync(string symbol, CancellationToken ct = default)
        => await ExecuteAsync(() => _primary.GetQuoteAsync(symbol, ct), () => _fallback.GetQuoteAsync(symbol, ct));

    public async Task<IReadOnlyList<StockTick>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        var symbolList = symbols.ToArray();
        return await ExecuteAsync(
            () => _primary.GetQuotesAsync(symbolList, ct),
            () => _fallback.GetQuotesAsync(symbolList, ct));
    }

    public async Task<IReadOnlyList<OhlcvBar>> GetOhlcvAsync(
        string symbol,
        string interval,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
        => await ExecuteAsync(
            () => _primary.GetOhlcvAsync(symbol, interval, fromUtc, toUtc, ct),
            () => _fallback.GetOhlcvAsync(symbol, interval, fromUtc, toUtc, ct));

    public async Task<StockInfo?> GetStockInfoAsync(string symbol, CancellationToken ct = default)
        => await ExecuteAsync(() => _primary.GetStockInfoAsync(symbol, ct), () => _fallback.GetStockInfoAsync(symbol, ct));

    public async Task<IReadOnlyList<StockInfo>> SearchSymbolsAsync(string query, CancellationToken ct = default)
        => await ExecuteAsync(() => _primary.SearchSymbolsAsync(query, ct), () => _fallback.SearchSymbolsAsync(query, ct));

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> primary, Func<Task<T>> fallback)
    {
        try
        {
            var value = await _pipeline.ExecuteAsync(async _ => await primary());
            return IsEmpty(value) ? await fallback() : value;
        }
        catch
        {
            return await fallback();
        }
    }

    private static bool IsEmpty<T>(T value)
        => value is null ||
           value is IReadOnlyCollection<StockTick> { Count: 0 } ||
           value is IReadOnlyCollection<OhlcvBar> { Count: 0 } ||
           value is IReadOnlyCollection<StockInfo> { Count: 0 };
}
