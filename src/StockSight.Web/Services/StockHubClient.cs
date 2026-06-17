using Microsoft.AspNetCore.SignalR.Client;
using StockSight.Core.Models;

namespace StockSight.Web.Services;

/// <summary>Simple wrapper for the API base address, injected via DI.</summary>
public record ApiSettings(string BaseUrl);

/// <summary>
/// Thin client over the SignalR <c>/hubs/stocks</c> hub used by Blazor pages.
/// </summary>
public class StockHubClient : IAsyncDisposable
{
    private readonly ApiSettings _settings;
    private HubConnection? _connection;

    public StockHubClient(ApiSettings settings) => _settings = settings;

    public event Action<StockTick>? TickReceived;
    public event Action<Alert>? AlertReceived;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_settings.BaseUrl.TrimEnd('/')}/hubs/stocks")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<StockTick>("ReceiveTick", tick => TickReceived?.Invoke(tick));
        _connection.On<Alert>("ReceiveAlert", alert => AlertReceived?.Invoke(alert));

        await _connection.StartAsync(ct);
    }

    public Task SubscribeAsync(string symbol)
        => _connection?.InvokeAsync("Subscribe", symbol) ?? Task.CompletedTask;

    public Task UnsubscribeAsync(string symbol)
        => _connection?.InvokeAsync("Unsubscribe", symbol) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
