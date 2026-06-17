using StockSight.Core.Models;

namespace StockSight.Core.Backtesting;

public interface IBacktestStrategy
{
    string Name { get; }

    bool ShouldBuy(IReadOnlyList<OhlcvBar> bars, int index);

    bool ShouldSell(IReadOnlyList<OhlcvBar> bars, int index);
}
