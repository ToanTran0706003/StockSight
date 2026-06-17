using Microsoft.EntityFrameworkCore;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;

namespace StockSight.Infrastructure.Watchlists;

public class WatchlistService : IWatchlistService
{
    private readonly StockSightDbContext _db;

    public WatchlistService(StockSightDbContext db) => _db = db;

    public async Task<IReadOnlyList<WatchlistItem>> GetAsync(Guid userId, CancellationToken ct = default)
        => await _db.WatchlistItems
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Symbol)
            .ToListAsync(ct);

    public async Task<WatchlistItem> AddAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        symbol = Clean(symbol);
        var existing = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
        if (existing is not null)
            return existing;

        var item = new WatchlistItem { UserId = userId, Symbol = symbol, AddedUtc = DateTime.UtcNow };
        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> RemoveAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        symbol = Clean(symbol);
        var item = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
        if (item is null)
            return false;

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default)
        => await _db.WatchlistItems
            .Select(w => w.Symbol)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

    private static string Clean(string symbol) => symbol.Trim().ToUpperInvariant();
}
