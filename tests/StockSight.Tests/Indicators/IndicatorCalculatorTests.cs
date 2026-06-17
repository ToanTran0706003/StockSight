using StockSight.Core.Indicators;
using StockSight.Core.Models;
using Xunit;

namespace StockSight.Tests.Indicators;

public class IndicatorCalculatorTests
{
    [Fact]
    public void CalculateSma_ReturnsNullUntilEnoughBars()
    {
        var bars = Bars(1, 2, 3, 4, 5);

        var sma = IndicatorCalculator.CalculateSma(bars, 3);

        Assert.Null(sma[1].Value);
        Assert.Equal(2m, sma[2].Value);
        Assert.Equal(4m, sma[4].Value);
    }

    [Fact]
    public void CalculateEma_SeedsWithSimpleAverage()
    {
        var bars = Bars(1, 2, 3, 4, 5);

        var ema = IndicatorCalculator.CalculateEma(bars, 3);

        Assert.Null(ema[1].Value);
        Assert.Equal(2m, ema[2].Value);
        Assert.Equal(3m, ema[3].Value);
        Assert.Equal(4m, ema[4].Value);
    }

    [Fact]
    public void CalculateRsi_ReturnsOneHundredForPureGains()
    {
        var bars = Bars(1, 2, 3, 4, 5, 6);

        var rsi = IndicatorCalculator.CalculateRsi(bars, 3);

        Assert.Null(rsi[2].Value);
        Assert.Equal(100m, rsi[3].Value);
        Assert.Equal(100m, rsi[5].Value);
    }

    [Fact]
    public void CalculateMacd_ProducesHistogramAfterSignalPeriod()
    {
        var bars = Bars(Enumerable.Range(1, 80).Select(i => (decimal)i).ToArray());

        var macd = IndicatorCalculator.CalculateMacd(bars);

        Assert.Contains(macd, p => p.Macd.HasValue);
        Assert.Contains(macd, p => p.Signal.HasValue);
        Assert.Contains(macd, p => p.Histogram.HasValue);
    }

    [Fact]
    public void CalculateBollingerBands_CentersBandsAroundSma()
    {
        var bars = Bars(1, 2, 3, 4, 5);

        var bands = IndicatorCalculator.CalculateBollingerBands(bars, 5, 2);

        Assert.Equal(3m, bands[4].Middle);
        Assert.True(bands[4].Upper > bands[4].Middle);
        Assert.True(bands[4].Lower < bands[4].Middle);
        Assert.Equal(bands[4].Upper - bands[4].Middle, bands[4].Middle - bands[4].Lower);
    }

    private static IReadOnlyList<OhlcvBar> Bars(params decimal[] closes)
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
