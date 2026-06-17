namespace StockSight.Core.Models;

public class OhlcvBar
{
    public string Symbol { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public string Interval { get; set; } = "1d";

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public long Volume { get; set; }
}
