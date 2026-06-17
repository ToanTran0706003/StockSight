namespace StockSight.Core.Models;

/// <summary>
/// A named collection of holdings owned by a user.
/// </summary>
public class Portfolio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Owner identifier (e.g. user id / subject claim).</summary>
    public string OwnerId { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal InitialCash { get; set; }

    public decimal CashBalance { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Positions held within this portfolio.</summary>
    public List<PortfolioHolding> Holdings { get; set; } = new();

    public List<PortfolioPosition> Positions { get; set; } = new();
}
