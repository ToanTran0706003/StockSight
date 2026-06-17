using StockSight.Core.Enums;

namespace StockSight.Core.Models;

public class TradeSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Symbol { get; set; } = string.Empty;

    public SignalAction Action { get; set; } = SignalAction.Hold;

    public decimal Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;

    public decimal? SentimentScore { get; set; }

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}
