using StockSight.Core.Indicators;
using StockSight.Core.Models;

namespace StockSight.Core.Backtesting.Strategies;

public class RsiReversalStrategy : IBacktestStrategy
{
    private readonly int _period;
    private readonly decimal _buyThreshold;
    private readonly decimal _sellThreshold;

    public RsiReversalStrategy(int period = 14, decimal buyThreshold = 30m, decimal sellThreshold = 70m)
    {
        _period = period;
        _buyThreshold = buyThreshold;
        _sellThreshold = sellThreshold;
    }

    public string Name => "RsiReversal";

    public bool ShouldBuy(IReadOnlyList<OhlcvBar> bars, int index)
    {
        var rsi = IndicatorCalculator.CalculateRsi(bars, _period);
        return index < rsi.Count && rsi[index].Value <= _buyThreshold;
    }

    public bool ShouldSell(IReadOnlyList<OhlcvBar> bars, int index)
    {
        var rsi = IndicatorCalculator.CalculateRsi(bars, _period);
        return index < rsi.Count && rsi[index].Value >= _sellThreshold;
    }
}
