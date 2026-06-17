using Microsoft.EntityFrameworkCore;
using StockSight.Core.Enums;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;

namespace StockSight.Infrastructure.Alerts;

public class AlertService : IAlertService
{
    private readonly StockSightDbContext _db;

    public AlertService(StockSightDbContext db) => _db = db;

    public async Task<IReadOnlyList<PriceAlert>> CheckAlertsAsync(StockTick tick, CancellationToken ct = default)
    {
        var symbol = tick.Symbol.Trim().ToUpperInvariant();
        var candidates = await _db.PriceAlerts
            .Where(a => a.Symbol == symbol && !a.IsTriggered)
            .ToListAsync(ct);

        var triggered = candidates
            .Where(a => a.Direction == AlertCondition.Above
                ? tick.Price >= a.TargetPrice
                : tick.Price <= a.TargetPrice)
            .ToList();

        foreach (var alert in triggered)
        {
            alert.IsTriggered = true;
            alert.TriggeredUtc = DateTime.UtcNow;
        }

        if (triggered.Count > 0)
            await _db.SaveChangesAsync(ct);

        return triggered;
    }

    public async Task<PriceAlert> CreateAlertAsync(PriceAlert alert, CancellationToken ct = default)
    {
        alert.Symbol = alert.Symbol.Trim().ToUpperInvariant();
        alert.CreatedUtc = DateTime.UtcNow;
        alert.IsTriggered = false;
        _db.PriceAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
        return alert;
    }

    public async Task DeleteAlertAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _db.PriceAlerts.FindAsync([alertId], ct);
        if (alert is null)
            return;

        _db.PriceAlerts.Remove(alert);
        await _db.SaveChangesAsync(ct);
    }
}
