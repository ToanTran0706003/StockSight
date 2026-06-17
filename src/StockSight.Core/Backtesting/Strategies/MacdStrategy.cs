using StockSight.Core.Indicators;
using StockSight.Core.Models;

namespace StockSight.Core.Backtesting.Strategies;

public class MacdStrategy : IBacktestStrategy
{
    public string Name => "Macd";

    public bool ShouldBuy(IReadOnlyList<OhlcvBar> bars, int index)
        => Crossed(bars, index, upward: true);

    public bool ShouldSell(IReadOnlyList<OhlcvBar> bars, int index)
        => Crossed(bars, index, upward: false);

    private static bool Crossed(IReadOnlyList<OhlcvBar> bars, int index, bool upward)
    {
        if (index <= 0)
            return false;

        var macd = IndicatorCalculator.CalculateMacd(bars);
        var previous = macd[index - 1];
        var current = macd[index];

        if (!previous.Macd.HasValue || !previous.Signal.HasValue || !current.Macd.HasValue || !current.Signal.HasValue)
            return false;

        return upward
            ? previous.Macd <= previous.Signal && current.Macd > current.Signal
            : previous.Macd >= previous.Signal && current.Macd < current.Signal;
    }
}
