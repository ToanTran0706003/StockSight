using StockSight.Core.Backtesting;
using StockSight.Core.Backtesting.Strategies;
using StockSight.Core.Models;
using Xunit;

namespace StockSight.Tests.Backtesting;

public class BacktestEngineTests
{
    [Fact]
    public void Run_RejectsInvalidCapital()
    {
        var engine = new BacktestEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Run(new SmaCrossoverStrategy(), "TEST", Bars(100, 101), 0));
    }

    [Fact]
    public void Run_ReturnsEquityCurveForEveryBar()
    {
        var engine = new BacktestEngine();
        var bars = Bars(Enumerable.Range(1, 90).Select(i => 100m + i));

        var result = engine.Run(new MacdStrategy(), "TEST", bars, 10_000m);

        Assert.Equal(bars.Count, result.EquityCurve.Count);
        Assert.Equal("TEST", result.Symbol);
    }

    [Fact]
    public void Run_BuyAndHoldStrategyCanProducePositiveReturn()
    {
        var engine = new BacktestEngine();
        var bars = Bars(Enumerable.Range(1, 20).Select(i => 100m + i));

        var result = engine.Run(new BuyAndHoldStrategy(), "TEST", bars, 10_000m);

        Assert.True(result.FinalValue > result.InitialCapital);
        Assert.Contains(result.Trades, t => t.Action == "BUY");
        Assert.Contains(result.Trades, t => t.Action == "SELL");
    }

    private class BuyAndHoldStrategy : IBacktestStrategy
    {
        public string Name => "BuyAndHold";

        public bool ShouldBuy(IReadOnlyList<OhlcvBar> bars, int index) => index == 0;

        public bool ShouldSell(IReadOnlyList<OhlcvBar> bars, int index) => false;
    }

    private static IReadOnlyList<OhlcvBar> Bars(params decimal[] closes) => Bars((IEnumerable<decimal>)closes);

    private static IReadOnlyList<OhlcvBar> Bars(IEnumerable<decimal> closes)
        => closes.Select((close, index) => new OhlcvBar
        {
            Symbol = "TEST",
            TimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
            Interval = "1d",
            Open = close,
            High = close,
            Low = close,
            Close = close,
            Volume = 1000
        }).ToArray();
}
