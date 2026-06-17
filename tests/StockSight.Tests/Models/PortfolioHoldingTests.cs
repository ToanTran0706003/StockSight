using StockSight.Core.Models;
using Xunit;

namespace StockSight.Tests.Models;

public class PortfolioHoldingTests
{
    [Fact]
    public void MarketValue_MultipliesQuantityByPrice()
    {
        var holding = new PortfolioHolding { Quantity = 10, AverageCost = 100 };

        Assert.Equal(1500m, holding.MarketValue(150));
    }

    [Fact]
    public void UnrealisedPnL_ComputesGainAndLoss()
    {
        var holding = new PortfolioHolding { Quantity = 10, AverageCost = 100 };

        Assert.Equal(500m, holding.UnrealisedPnL(150));   // gain
        Assert.Equal(-200m, holding.UnrealisedPnL(80));   // loss
    }
}
