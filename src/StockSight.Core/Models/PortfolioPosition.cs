namespace StockSight.Core.Models;

public class PortfolioPosition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PortfolioId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal Shares { get; set; }

    public decimal AverageCost { get; set; }

    public DateTime BoughtUtc { get; set; } = DateTime.UtcNow;

    public Portfolio? Portfolio { get; set; }
}
