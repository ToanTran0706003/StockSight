using Microsoft.EntityFrameworkCore;
using Moq;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;
using StockSight.Infrastructure.Portfolios;
using Xunit;

namespace StockSight.Tests.Portfolio;

public class PortfolioServiceTests
{
    [Fact]
    public async Task BuyAsync_UpdatesCashAndAverageCost()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var portfolio = new StockSight.Core.Models.Portfolio
        {
            UserId = userId,
            OwnerId = userId.ToString(),
            Name = "Test",
            InitialCash = 10_000m,
            CashBalance = 10_000m
        };
        db.Portfolios.Add(portfolio);
        await db.SaveChangesAsync();

        var service = new PortfolioService(db, Mock.Of<IStockDataProvider>());
        await service.BuyAsync(portfolio.Id, "AAPL", 10, 100);
        await service.BuyAsync(portfolio.Id, "AAPL", 10, 120);

        var saved = await db.Portfolios.Include(p => p.Positions).SingleAsync();
        Assert.Equal(7_800m, saved.CashBalance);
        Assert.Single(saved.Positions);
        Assert.Equal(20m, saved.Positions[0].Shares);
        Assert.Equal(110m, saved.Positions[0].AverageCost);
    }

    [Fact]
    public async Task SellAsync_RejectsOversell()
    {
        using var db = CreateDb();
        var portfolio = new StockSight.Core.Models.Portfolio
        {
            UserId = Guid.NewGuid(),
            OwnerId = "user",
            Name = "Test",
            InitialCash = 1_000m,
            CashBalance = 500m,
            Positions = [new PortfolioPosition { Symbol = "MSFT", Shares = 2, AverageCost = 250 }]
        };
        db.Portfolios.Add(portfolio);
        await db.SaveChangesAsync();

        var service = new PortfolioService(db, Mock.Of<IStockDataProvider>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SellAsync(portfolio.Id, "MSFT", 3, 260));
    }

    [Fact]
    public async Task GetSnapshotAsync_UsesCurrentPricesForPnL()
    {
        using var db = CreateDb();
        var portfolio = new StockSight.Core.Models.Portfolio
        {
            UserId = Guid.NewGuid(),
            OwnerId = "user",
            Name = "Test",
            InitialCash = 1_000m,
            CashBalance = 500m,
            Positions = [new PortfolioPosition { Symbol = "NVDA", Shares = 2, AverageCost = 250 }]
        };
        db.Portfolios.Add(portfolio);
        await db.SaveChangesAsync();

        var provider = new Mock<IStockDataProvider>();
        provider.Setup(p => p.GetQuoteAsync("NVDA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockTick { Symbol = "NVDA", Price = 300 });

        var service = new PortfolioService(db, provider.Object);
        var snapshot = await service.GetSnapshotAsync(portfolio.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(1_100m, snapshot!.TotalValue);
        Assert.Equal(100m, snapshot.TotalPnL);
        Assert.Equal(20m, snapshot.Positions[0].PnLPercent);
    }

    private static StockSightDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StockSightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StockSightDbContext(options);
    }
}
