namespace StockSight.Core.Models;

/// <summary>
/// A single point-in-time price observation for a ticker symbol.
/// Broadcast over SignalR and cached in Redis as the latest known quote.
/// </summary>
public class StockTick
{
    /// <summary>Ticker symbol, e.g. "AAPL". Always stored upper-cased.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Last traded price.</summary>
    public decimal Price { get; set; }

    /// <summary>Absolute change versus the previous close.</summary>
    public decimal Change { get; set; }

    /// <summary>Percentage change versus the previous close.</summary>
    public decimal ChangePercent { get; set; }

    /// <summary>Traded volume for the session.</summary>
    public long Volume { get; set; }

    /// <summary>UTC timestamp the tick was observed.</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
