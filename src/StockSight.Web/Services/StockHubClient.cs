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
    private bool _disposing;

    public StockHubClient(ApiSettings settings) => _settings = settings;

    public event Action<StockTick>? TickReceived;
    public event Action<Alert>? AlertReceived;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection is { State: not HubConnectionState.Disconnected })
            return;

        if (_connection is not null)
        {
            await DisposeConnectionAsync();
        }

        _disposing = false;
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_settings.BaseUrl.TrimEnd('/')}/hubs/stocks")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<StockTick>("ReceiveTick", tick => TickReceived?.Invoke(tick));
        _connection.On<Alert>("ReceiveAlert", alert => AlertReceived?.Invoke(alert));

        await _connection.StartAsync(ct);
    }

    public async Task SubscribeAsync(string symbol)
    {
        var connection = _connection;
        if (_disposing || connection is null || connection.State == HubConnectionState.Disconnected)
            return;

        try
        {
            await connection.InvokeAsync("Subscribe", symbol);
        }
        catch (ObjectDisposedException)
        {
            _connection = null;
        }
        catch (InvalidOperationException) when (connection.State == HubConnectionState.Disconnected)
        {
        }
    }

    public async Task UnsubscribeAsync(string symbol)
    {
        var connection = _connection;
        if (_disposing || connection is null || connection.State == HubConnectionState.Disconnected)
            return;

        try
        {
            await connection.InvokeAsync("Unsubscribe", symbol);
        }
        catch (ObjectDisposedException)
        {
            _connection = null;
        }
        catch (InvalidOperationException) when (connection.State == HubConnectionState.Disconnected)
        {
        }
    }

    public async ValueTask DisposeAsync()
        => await DisposeConnectionAsync();

    private async ValueTask DisposeConnectionAsync()
    {
        var connection = _connection;
        if (connection is null)
            return;

        _disposing = true;
        _connection = null;
        try
        {
            await connection.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
