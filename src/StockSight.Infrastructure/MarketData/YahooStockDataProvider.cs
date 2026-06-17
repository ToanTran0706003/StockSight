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

    private static decimal ToDecimal(dynamic? value)
        => value is null ? 0m : Convert.ToDecimal(value);

    private static long ToLong(dynamic? value)
        => value is null ? 0L : Convert.ToInt64(value);
}
