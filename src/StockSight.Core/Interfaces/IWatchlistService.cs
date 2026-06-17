using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItem>> GetAsync(Guid userId, CancellationToken ct = default);

    Task<WatchlistItem> AddAsync(Guid userId, string symbol, CancellationToken ct = default);

    Task<bool> RemoveAsync(Guid userId, string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default);
}
