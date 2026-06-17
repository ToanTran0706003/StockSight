using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface IAlertService
{
    Task<IReadOnlyList<PriceAlert>> CheckAlertsAsync(StockTick tick, CancellationToken ct = default);

    Task<PriceAlert> CreateAlertAsync(PriceAlert alert, CancellationToken ct = default);

    Task DeleteAlertAsync(Guid alertId, CancellationToken ct = default);
}
