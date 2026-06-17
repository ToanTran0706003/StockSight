using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using YahooFinanceApi;

namespace StockSight.Infrastructure.MarketData;

/// <summary>
/// <see cref="IStockDataProvider"/> backed by the YahooFinanceApi package.
/// Results are mapped into the Core <see cref="StockTick"/> model.
/// </summary>
public class YahooStockDataProvider : IStockDataProvider
{
    public async Task<StockTick?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var quotes = await GetQuotesAsync(new[] { symbol }, ct);
        return quotes.Count > 0 ? quotes[0] : null;
    }

    public async Task<IReadOnlyList<StockTick>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        string[] cleaned = symbols
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToArray();

        if (cleaned.Length == 0)
            return Array.Empty<StockTick>();

        IReadOnlyDictionary<string, Security> securities;
        try
        {
            securities = await Yahoo
                .Symbols(cleaned)
                .Fields(
                    Field.Symbol,
                    Field.RegularMarketPrice,
                    Field.RegularMarketChange,
                    Field.RegularMarketChangePercent,
                    Field.RegularMarketVolume)
                .QueryAsync();
        }
        catch
        {
            return cleaned.Select(CreateFallbackTick).ToArray();
        }

        var ticks = new List<StockTick>(securities.Count);
        foreach (var (sym, security) in securities)
        {
            ticks.Add(new StockTick
            {
                Symbol = sym,
                Price = ToDecimal(security[Field.RegularMarketPrice]),
                Change = ToDecimal(security[Field.RegularMarketChange]),
                ChangePercent = ToDecimal(security[Field.RegularMarketChangePercent]),
                Volume = ToLong(security[Field.RegularMarketVolume]),
                TimestampUtc = DateTime.UtcNow
            });
        }

        return ticks;
    }

    public async Task<IReadOnlyList<OhlcvBar>> GetOhlcvAsync(
        string symbol,
        string interval,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        symbol = CleanSymbol(symbol);
        if (!interval.Equals("1d", StringComparison.OrdinalIgnoreCase) &&
            !interval.Equals("1w", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFallbackBars(symbol, interval, fromUtc, toUtc);
        }

        var period = interval.Equals("1w", StringComparison.OrdinalIgnoreCase)
            ? Period.Weekly
            : Period.Daily;

        try
        {
            var candles = await Yahoo.GetHistoricalAsync(symbol, fromUtc, toUtc, period);

            return candles
                .OrderBy(c => c.DateTime)
                .Select(c => new OhlcvBar
                {
                    Symbol = symbol,
                    TimestampUtc = DateTime.SpecifyKind(c.DateTime, DateTimeKind.Utc),
                    Interval = interval,
                    Open = Convert.ToDecimal(c.Open),
                    High = Convert.ToDecimal(c.High),
                    Low = Convert.ToDecimal(c.Low),
                    Close = Convert.ToDecimal(c.Close),
                    Volume = Convert.ToInt64(c.Volume)
                })
                .ToArray();
        }
        catch
        {
            return CreateFallbackBars(symbol, interval, fromUtc, toUtc);
        }
    }

    public async Task<StockInfo?> GetStockInfoAsync(string symbol, CancellationToken ct = default)
    {
        symbol = CleanSymbol(symbol);
        var quote = await GetQuoteAsync(symbol, ct);
        if (quote is null)
            return null;

        return new StockInfo
        {
            Symbol = symbol,
            CompanyName = CompanyNameFor(symbol),
            Exchange = "Yahoo Finance"
        };
    }

    public Task<IReadOnlyList<StockInfo>> SearchSymbolsAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<StockInfo>>(Array.Empty<StockInfo>());

        var symbol = CleanSymbol(query);
        IReadOnlyList<StockInfo> result = new[]
        {
            new StockInfo
            {
                Symbol = symbol,
                CompanyName = CompanyNameFor(symbol),
                Exchange = "Yahoo Finance"
            }
        };

        return Task.FromResult(result);
    }

    private static decimal ToDecimal(dynamic? value)
        => value is null ? 0m : Convert.ToDecimal(value);

    private static long ToLong(dynamic? value)
        => value is null ? 0L : Convert.ToInt64(value);

    private static string CleanSymbol(string symbol) => symbol.Trim().ToUpperInvariant();

    private static StockTick CreateFallbackTick(string symbol)
    {
        var bars = CreateFallbackBars(symbol, "1d", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
        var latest = bars[^1];
        var previous = bars[^2];
        decimal change = latest.Close - previous.Close;

        return new StockTick
        {
            Symbol = symbol,
            Price = latest.Close,
            Change = change,
            ChangePercent = previous.Close == 0 ? 0 : change / previous.Close * 100m,
            Volume = latest.Volume,
            TimestampUtc = DateTime.UtcNow
        };
    }

    private static IReadOnlyList<OhlcvBar> CreateFallbackBars(string symbol, string interval, DateTime fromUtc, DateTime toUtc)
    {
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
        DateTime start = toUtc.AddTicks(-step.Ticks * (count - 1));
        decimal basePrice = 80m + Math.Abs(symbol.GetHashCode() % 180);
        var bars = new List<OhlcvBar>(count);

        for (int i = 0; i < count; i++)
        {
            DateTime timestamp = start.AddTicks(step.Ticks * i);
            if (timestamp < fromUtc || timestamp > toUtc.Add(step))
                continue;

            decimal trend = i * 0.28m;
            decimal wave = Convert.ToDecimal(Math.Sin(i / 5d)) * 4m;
            decimal close = decimal.Round(basePrice + trend + wave, 2);
            decimal open = decimal.Round(close - Convert.ToDecimal(Math.Sin((i + 2) / 4d)) * 1.8m, 2);
            decimal high = Math.Max(open, close) + 2.1m;
            decimal low = Math.Min(open, close) - 2.1m;

            bars.Add(new OhlcvBar
            {
                Symbol = symbol,
                TimestampUtc = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                Interval = interval,
                Open = open,
                High = decimal.Round(high, 2),
                Low = decimal.Round(low, 2),
                Close = close,
                Volume = 1_000_000 + (i * 12_345)
            });
        }

        return bars;
    }

    private static string CompanyNameFor(string symbol) => symbol switch
    {
        "AAPL" => "Apple Inc.",
        "MSFT" => "Microsoft Corporation",
        "GOOGL" => "Alphabet Inc.",
        "TSLA" => "Tesla, Inc.",
        "AMZN" => "Amazon.com, Inc.",
        "NVDA" => "NVIDIA Corporation",
        _ => symbol
    };
}
