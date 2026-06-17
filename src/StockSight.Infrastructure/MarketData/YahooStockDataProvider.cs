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

        IReadOnlyDictionary<string, Security> securities = await Yahoo
            .Symbols(cleaned)
            .Fields(
                Field.Symbol,
                Field.RegularMarketPrice,
                Field.RegularMarketChange,
                Field.RegularMarketChangePercent,
                Field.RegularMarketVolume)
            .QueryAsync();

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
        var period = interval.Equals("1w", StringComparison.OrdinalIgnoreCase)
            ? Period.Weekly
            : Period.Daily;

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

    public async Task<StockInfo?> GetStockInfoAsync(string symbol, CancellationToken ct = default)
    {
        symbol = CleanSymbol(symbol);
        var quote = await GetQuoteAsync(symbol, ct);
        if (quote is null)
            return null;

        return new StockInfo
        {
            Symbol = symbol,
            CompanyName = symbol,
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
                CompanyName = symbol,
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
}
