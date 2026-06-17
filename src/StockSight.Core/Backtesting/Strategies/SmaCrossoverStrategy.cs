using StockSight.Core.Indicators;
using StockSight.Core.Models;

namespace StockSight.Core.Backtesting.Strategies;

public class SmaCrossoverStrategy : IBacktestStrategy
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;

    public SmaCrossoverStrategy(int fastPeriod = 20, int slowPeriod = 50)
    {
        if (fastPeriod >= slowPeriod)
            throw new ArgumentException("Fast period must be lower than slow period.");

        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
    }

    public string Name => "SmaCrossover";

    public bool ShouldBuy(IReadOnlyList<OhlcvBar> bars, int index)
        => Crossed(index, bars, upward: true);

    public bool ShouldSell(IReadOnlyList<OhlcvBar> bars, int index)
        => Crossed(index, bars, upward: false);

    private bool Crossed(int index, IReadOnlyList<OhlcvBar> bars, bool upward)
    {
        if (index <= _slowPeriod)
            return false;

        var fast = IndicatorCalculator.CalculateSma(bars, _fastPeriod);
        var slow = IndicatorCalculator.CalculateSma(bars, _slowPeriod);
        var previousFast = fast[index - 1].Value;
        var previousSlow = slow[index - 1].Value;
        var currentFast = fast[index].Value;
        var currentSlow = slow[index].Value;

        if (!previousFast.HasValue || !previousSlow.HasValue || !currentFast.HasValue || !currentSlow.HasValue)
            return false;

        return upward
            ? previousFast <= previousSlow && currentFast > currentSlow
            : previousFast >= previousSlow && currentFast < currentSlow;
    }
}
