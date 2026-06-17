using StockSight.Core.Enums;
using StockSight.Core.Models;
using StockSight.Core.Signals;
using Xunit;

namespace StockSight.Tests.Signals;

public class RuleBasedSignalAnalyzerTests
{
    [Fact]
    public void Analyze_ReturnsHoldWhenHistoryIsTooShort()
    {
        var analyzer = new RuleBasedSignalAnalyzer();

        var result = analyzer.Analyze(Bars(Enumerable.Range(1, 10).Select(i => (decimal)i)));

        Assert.Equal(SignalAction.Hold, result.Action);
    }

    [Fact]
    public void Analyze_ProducesSellWhenPriceIsExtended()
    {
        var analyzer = new RuleBasedSignalAnalyzer();
        var prices = Enumerable.Range(1, 80).Select(i => 100m + i).ToArray();

        var result = analyzer.Analyze(Bars(prices));

        Assert.Equal(SignalAction.Sell, result.Action);
        Assert.True(result.Confidence >= 50m);
    }

    [Fact]
    public void Analyze_ProducesBuyWhenPriceDropsToLowerBand()
    {
        var analyzer = new RuleBasedSignalAnalyzer();
        var prices = Enumerable.Range(1, 70).Select(_ => 100m).Concat(new[] { 75m }).ToArray();

        var result = analyzer.Analyze(Bars(prices));

        Assert.Equal(SignalAction.Buy, result.Action);
    }

    private static IReadOnlyList<OhlcvBar> Bars(IEnumerable<decimal> closes)
        => closes.Select((close, index) => new OhlcvBar
        {
            Symbol = "TEST",
            TimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
            Interval = "1d",
            Open = close,
            High = close + 1,
            Low = close - 1,
            Close = close,
            Volume = 1000
        }).ToArray();
}
