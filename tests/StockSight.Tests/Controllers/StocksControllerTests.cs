using Microsoft.AspNetCore.Mvc;
using Moq;
using StockSight.API.Controllers;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using Xunit;

namespace StockSight.Tests.Controllers;

public class StocksControllerTests
{
    [Fact]
    public async Task GetQuote_ReturnsCachedValue_WithoutHittingProvider()
    {
        var cached = new StockTick { Symbol = "AAPL", Price = 200 };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<StockTick>("quote:AAPL", It.IsAny<CancellationToken>()))
             .ReturnsAsync(cached);
        var provider = new Mock<IStockDataProvider>();

        var controller = new StocksController(provider.Object, cache.Object);

        var result = await controller.GetQuote("aapl", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(cached, ok.Value);
        provider.Verify(p => p.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetQuote_OnCacheMiss_FetchesAndCaches()
    {
        var fresh = new StockTick { Symbol = "MSFT", Price = 400 };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<StockTick>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StockTick?)null);
        var provider = new Mock<IStockDataProvider>();
        provider.Setup(p => p.GetQuoteAsync("MSFT", It.IsAny<CancellationToken>()))
                .ReturnsAsync(fresh);

        var controller = new StocksController(provider.Object, cache.Object);

        var result = await controller.GetQuote("msft", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(fresh, ok.Value);
        cache.Verify(c => c.SetAsync("quote:MSFT", fresh, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
