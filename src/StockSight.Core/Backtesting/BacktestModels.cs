namespace StockSight.Core.Backtesting;

public record BacktestRequest(
    string Symbol,
    string Strategy,
    DateTime From,
    DateTime To,
    decimal InitialCapital);

public record EquityPoint(DateTime Date, decimal Value);

public record BacktestTrade(
    DateTime Date,
    string Action,
    decimal Price,
    decimal Shares,
    decimal Value,
    decimal? PnL);

public class BacktestResult
{
    public string Symbol { get; set; } = string.Empty;

    public string Strategy { get; set; } = string.Empty;

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public decimal InitialCapital { get; set; }

    public decimal FinalValue { get; set; }

    public decimal TotalReturnPercent { get; set; }

    public decimal SharpeRatio { get; set; }

    public decimal MaxDrawdownPercent { get; set; }

    public decimal WinRate { get; set; }

    public int TotalTrades { get; set; }

    public int WinningTrades { get; set; }

    public int LosingTrades { get; set; }

    public List<EquityPoint> EquityCurve { get; set; } = new();

    public List<BacktestTrade> Trades { get; set; } = new();
}
