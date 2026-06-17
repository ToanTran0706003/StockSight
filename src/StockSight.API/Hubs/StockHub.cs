using Microsoft.AspNetCore.SignalR;
using StockSight.Core.Models;

namespace StockSight.API.Hubs;

/// <summary>
/// Real-time hub. Clients join per-symbol groups and receive "ReceiveTick" /
/// "ReceiveAlert" messages pushed from the server.
/// </summary>
public class StockHub : Hub
{
    /// <summary>Subscribe the caller to live updates for a symbol.</summary>
    public Task Subscribe(string symbol)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(symbol));

    public Task SubscribeToSymbol(string symbol) => Subscribe(symbol);

    /// <summary>Unsubscribe the caller from a symbol's updates.</summary>
    public Task Unsubscribe(string symbol)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(symbol));

    public Task UnsubscribeFromSymbol(string symbol) => Unsubscribe(symbol);

    /// <summary>Group name convention for a symbol.</summary>
    public static string GroupFor(string symbol) => $"symbol:{symbol.Trim().ToUpperInvariant()}";

    // Strongly-typed message names used by clients.
    public const string ReceiveTick = "ReceiveTick";
    public const string ReceiveAlert = "ReceiveAlert";
}
