using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.Infrastructure.MarketData;

public class MockStockDataProvider : IStockDataProvider
{
    public Task<StockTick?> GetQuoteAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult<StockTick?>(CreateTick(Clean(symbol)));

    public Task<IReadOnlyList<StockTick>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<StockTick>>(symbols.Select(s => CreateTick(Clean(s))).ToArray());

    public Task<IReadOnlyList<OhlcvBar>> GetOhlcvAsync(
        string symbol,
        string interval,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        symbol = Clean(symbol);
        (int count, TimeSpan step) = interval.ToLowerInvariant() switch
        {
            "1m" => (240, TimeSpan.FromMinutes(1)),
            "5m" => (240, TimeSpan.FromMinutes(5)),
            "15m" => (240, TimeSpan.FromMinutes(15)),
            "1h" => (240, TimeSpan.FromHours(1)),
            "4h" => (180, TimeSpan.FromHours(4)),
            "1w" => (104, TimeSpan.FromDays(7)),
            _ => (180, TimeSpan.FromDays(1))
        };

        var start = toUtc.AddTicks(-step.Ticks * (count - 1));
        var basePrice = BasePrice(symbol);
        var bars = Enumerable.Range(0, count)
            .Select(i =>
            {
                var close = decimal.Round(basePrice + (i * 0.25m) + Convert.ToDecimal(Math.Sin(i / 6d)) * 3m, 2);
                var open = decimal.Round(close - Convert.ToDecimal(Math.Cos(i / 5d)) * 1.2m, 2);
                return new OhlcvBar
                {
                    Symbol = symbol,
                    Interval = interval,
                    TimestampUtc = DateTime.SpecifyKind(start.AddTicks(step.Ticks * i), DateTimeKind.Utc),
                    Open = open,
                    High = decimal.Round(Math.Max(open, close) + 1.8m, 2),
                    Low = decimal.Round(Math.Min(open, close) - 1.8m, 2),
                    Close = close,
                    Volume = 900_000 + i * 10_000
                };
            })
            .Where(b => b.TimestampUtc >= fromUtc && b.TimestampUtc <= toUtc.Add(step))
            .ToArray();

        return Task.FromResult<IReadOnlyList<OhlcvBar>>(bars);
    }

    public Task<StockInfo?> GetStockInfoAsync(string symbol, CancellationToken ct = default)
    {
        symbol = Clean(symbol);
        return Task.FromResult<StockInfo?>(new StockInfo
        {
            Symbol = symbol,
            CompanyName = symbol switch
            {
                "AAPL" => "Apple Inc.",
                "MSFT" => "Microsoft Corporation",
                "GOOGL" => "Alphabet Inc.",
                "TSLA" => "Tesla, Inc.",
                "AMZN" => "Amazon.com, Inc.",
                "NVDA" => "NVIDIA Corporation",
                _ => $"{symbol} Corporation"
            },
            Exchange = "Mock Market"
        });
    }

    public Task<IReadOnlyList<StockInfo>> SearchSymbolsAsync(string query, CancellationToken ct = default)
    {
        var symbol = Clean(query);
        return Task.FromResult<IReadOnlyList<StockInfo>>(
            string.IsNullOrWhiteSpace(symbol)
                ? Array.Empty<StockInfo>()
                : new[] { new StockInfo { Symbol = symbol, CompanyName = $"{symbol} Corporation", Exchange = "Mock Market" } });
    }

    private static StockTick CreateTick(string symbol)
    {
        var price = BasePrice(symbol) + decimal.Round(Convert.ToDecimal(Math.Sin(DateTime.UtcNow.Minute / 4d)) * 2m, 2);
        return new StockTick
        {
            Symbol = symbol,
            Price = price,
            Change = 1.25m,
            ChangePercent = 0.52m,
            Volume = 1_250_000,
            TimestampUtc = DateTime.UtcNow
        };
    }

    private static decimal BasePrice(string symbol) => 80m + Math.Abs(symbol.GetHashCode() % 220);

    private static string Clean(string symbol) => symbol.Trim().ToUpperInvariant();
}
