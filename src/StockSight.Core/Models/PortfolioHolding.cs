namespace StockSight.Core.Models;

/// <summary>
/// A single position (quantity of one symbol at an average cost) inside a <see cref="Portfolio"/>.
/// </summary>
public class PortfolioHolding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PortfolioId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Average purchase price per share.</summary>
    public decimal AverageCost { get; set; }

    /// <summary>Navigation back to the owning portfolio.</summary>
    public Portfolio? Portfolio { get; set; }

    /// <summary>Market value at a given last price.</summary>
    public decimal MarketValue(decimal lastPrice) => Quantity * lastPrice;

    /// <summary>Unrealised profit/loss at a given last price.</summary>
    public decimal UnrealisedPnL(decimal lastPrice) => (lastPrice - AverageCost) * Quantity;
}
