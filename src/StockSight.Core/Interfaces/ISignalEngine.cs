using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface ISignalEngine
{
    Task<TradeSignal> AnalyzeAsync(string symbol, CancellationToken ct = default);
}
