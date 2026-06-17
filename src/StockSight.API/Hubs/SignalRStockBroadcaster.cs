using Microsoft.AspNetCore.SignalR;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.Hubs;

/// <summary>
/// <see cref="IStockBroadcaster"/> implemented over <see cref="StockHub"/> so
/// background jobs and services can push updates without referencing SignalR.
/// </summary>
public class SignalRStockBroadcaster : IStockBroadcaster
{
    private readonly IHubContext<StockHub> _hub;

    public SignalRStockBroadcaster(IHubContext<StockHub> hub) => _hub = hub;

    public Task BroadcastTickAsync(StockTick tick, CancellationToken ct = default)
        => _hub.Clients.Group(StockHub.GroupFor(tick.Symbol))
               .SendAsync(StockHub.ReceiveTick, tick, ct);

    public Task BroadcastAlertAsync(Alert alert, CancellationToken ct = default)
        => _hub.Clients.Group(StockHub.GroupFor(alert.Symbol))
               .SendAsync(StockHub.ReceiveAlert, alert, ct);
}
