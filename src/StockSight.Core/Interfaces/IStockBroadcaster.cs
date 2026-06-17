using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

/// <summary>
/// Pushes real-time updates to connected clients. Implemented in the API layer
/// over the SignalR hub; declared here so background jobs can depend on the
/// abstraction rather than on SignalR types.
/// </summary>
public interface IStockBroadcaster
{
    Task BroadcastTickAsync(StockTick tick, CancellationToken ct = default);

    Task BroadcastAlertAsync(Alert alert, CancellationToken ct = default);
}
