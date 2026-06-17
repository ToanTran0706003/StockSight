using Microsoft.EntityFrameworkCore;
using StockSight.Core.Enums;
using StockSight.Core.Models;
using StockSight.Infrastructure.Alerts;
using StockSight.Infrastructure.Data;
using Xunit;

namespace StockSight.Tests.Alerts;

public class AlertServiceTests
{
    [Fact]
    public async Task CheckAlertsAsync_TriggersMatchingAlertsOnly()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.PriceAlerts.AddRange(
            new PriceAlert { UserId = userId, Symbol = "AAPL", TargetPrice = 200, Direction = AlertCondition.Above },
            new PriceAlert { UserId = userId, Symbol = "AAPL", TargetPrice = 150, Direction = AlertCondition.Below },
            new PriceAlert { UserId = userId, Symbol = "MSFT", TargetPrice = 300, Direction = AlertCondition.Above });
        await db.SaveChangesAsync();

        var service = new AlertService(db);
        var triggered = await service.CheckAlertsAsync(new StockTick { Symbol = "AAPL", Price = 210 });

        Assert.Single(triggered);
        Assert.Equal(200m, triggered[0].TargetPrice);
        Assert.True(triggered[0].IsTriggered);
        Assert.NotNull(triggered[0].TriggeredUtc);
        Assert.Equal(1, await db.PriceAlerts.CountAsync(a => a.IsTriggered));
    }

    private static StockSightDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StockSightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StockSightDbContext(options);
    }
}
