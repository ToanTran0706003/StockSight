using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

/// <summary>
/// Source of market data (e.g. Yahoo Finance). Implemented in Infrastructure.
/// </summary>
public interface IStockDataProvider
{
    /// <summary>Fetch the latest quote for a single symbol.</summary>
    Task<StockTick?> GetQuoteAsync(string symbol, CancellationToken ct = default);

    /// <summary>Fetch the latest quotes for several symbols at once.</summary>
    Task<IReadOnlyList<StockTick>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default);
}
