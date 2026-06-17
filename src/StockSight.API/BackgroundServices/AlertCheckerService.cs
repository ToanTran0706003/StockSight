using StockSight.Core.Enums;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.BackgroundServices;

public class AlertCheckerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertCheckerService> _logger;

    public AlertCheckerService(IServiceScopeFactory scopeFactory, ILogger<AlertCheckerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var watchlist = scope.ServiceProvider.GetRequiredService<IWatchlistService>();
                var provider = scope.ServiceProvider.GetRequiredService<IStockDataProvider>();
                var alerts = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var broadcaster = scope.ServiceProvider.GetRequiredService<IStockBroadcaster>();

                foreach (var symbol in await watchlist.GetAllSymbolsAsync(stoppingToken))
                {
                    var tick = await provider.GetQuoteAsync(symbol, stoppingToken);
                    if (tick is null)
                        continue;

                    var triggered = await alerts.CheckAlertsAsync(tick, stoppingToken);
                    foreach (var alert in triggered)
                    {
                        await broadcaster.BroadcastAlertAsync(new Alert
                        {
                            Id = alert.Id,
                            OwnerId = alert.UserId.ToString(),
                            Symbol = alert.Symbol,
                            TargetPrice = alert.TargetPrice,
                            Condition = alert.Direction,
                            Status = AlertStatus.Triggered,
                            CreatedUtc = alert.CreatedUtc,
                            TriggeredUtc = alert.TriggeredUtc
                        }, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alert checker loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
