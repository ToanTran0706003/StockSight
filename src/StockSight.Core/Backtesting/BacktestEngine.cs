using StockSight.Core.Models;

namespace StockSight.Core.Backtesting;

public class BacktestEngine
{
    public BacktestResult Run(
        IBacktestStrategy strategy,
        string symbol,
        IReadOnlyList<OhlcvBar> bars,
        decimal initialCapital)
    {
        if (initialCapital <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapital), "Initial capital must be positive.");

        if (bars.Count == 0)
            throw new ArgumentException("Backtest requires at least one candle.", nameof(bars));

        decimal cash = initialCapital;
        decimal shares = 0m;
        decimal? entryValue = null;
        var equityCurve = new List<EquityPoint>(bars.Count);
        var trades = new List<BacktestTrade>();

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            decimal price = bar.Close;

            if (shares == 0m && strategy.ShouldBuy(bars, i))
            {
                shares = cash / price;
                entryValue = cash;
                trades.Add(new BacktestTrade(bar.TimestampUtc, "BUY", price, shares, cash, null));
                cash = 0m;
            }
            else if (shares > 0m && strategy.ShouldSell(bars, i))
            {
                decimal value = shares * price;
                decimal pnl = entryValue.HasValue ? value - entryValue.Value : 0m;
                cash = value;
                trades.Add(new BacktestTrade(bar.TimestampUtc, "SELL", price, shares, value, pnl));
                shares = 0m;
                entryValue = null;
            }

            equityCurve.Add(new EquityPoint(bar.TimestampUtc, cash + (shares * price)));
        }

        if (shares > 0m)
        {
            var last = bars[^1];
            decimal value = shares * last.Close;
            decimal pnl = entryValue.HasValue ? value - entryValue.Value : 0m;
            cash = value;
            trades.Add(new BacktestTrade(last.TimestampUtc, "SELL", last.Close, shares, value, pnl));
            shares = 0m;
        }

        decimal finalValue = cash;
        int closedTrades = trades.Count(t => t.Action == "SELL");
        int wins = trades.Count(t => t.PnL > 0);
        int losses = trades.Count(t => t.PnL < 0);

        return new BacktestResult
        {
            Symbol = symbol.Trim().ToUpperInvariant(),
            Strategy = strategy.Name,
            From = bars[0].TimestampUtc,
            To = bars[^1].TimestampUtc,
            InitialCapital = initialCapital,
            FinalValue = decimal.Round(finalValue, 2),
            TotalReturnPercent = decimal.Round((finalValue - initialCapital) / initialCapital * 100m, 2),
            SharpeRatio = decimal.Round(CalculateSharpe(equityCurve), 2),
            MaxDrawdownPercent = decimal.Round(CalculateMaxDrawdown(equityCurve), 2),
            WinRate = closedTrades == 0 ? 0m : decimal.Round((decimal)wins / closedTrades * 100m, 2),
            TotalTrades = trades.Count,
            WinningTrades = wins,
            LosingTrades = losses,
            EquityCurve = equityCurve,
            Trades = trades
        };
    }

    private static decimal CalculateMaxDrawdown(IReadOnlyList<EquityPoint> equityCurve)
    {
        decimal peak = equityCurve[0].Value;
        decimal maxDrawdown = 0m;

        foreach (var point in equityCurve)
        {
            peak = Math.Max(peak, point.Value);
            if (peak > 0)
                maxDrawdown = Math.Min(maxDrawdown, (point.Value - peak) / peak * 100m);
        }

        return maxDrawdown;
    }

    private static decimal CalculateSharpe(IReadOnlyList<EquityPoint> equityCurve)
    {
        if (equityCurve.Count < 3)
            return 0m;

        var returns = new List<decimal>();
        for (int i = 1; i < equityCurve.Count; i++)
        {
            decimal previous = equityCurve[i - 1].Value;
            if (previous > 0)
                returns.Add((equityCurve[i].Value - previous) / previous);
        }

        if (returns.Count < 2)
            return 0m;

        decimal average = returns.Average();
        decimal variance = returns.Sum(r => (r - average) * (r - average)) / (returns.Count - 1);
        decimal standardDeviation = Convert.ToDecimal(Math.Sqrt(Convert.ToDouble(variance)));

        return standardDeviation == 0m
            ? 0m
            : average / standardDeviation * Convert.ToDecimal(Math.Sqrt(252));
    }
}
