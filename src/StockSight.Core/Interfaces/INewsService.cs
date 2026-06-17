namespace StockSight.Core.Interfaces;

public interface INewsService
{
    Task<IReadOnlyList<string>> GetNewsBySymbolAsync(string symbol, CancellationToken ct = default);

    Task<decimal?> GetSentimentAsync(string symbol, CancellationToken ct = default);
}
