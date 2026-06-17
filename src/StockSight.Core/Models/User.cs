namespace StockSight.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<WatchlistItem> WatchlistItems { get; set; } = new();

    public List<Portfolio> Portfolios { get; set; } = new();

    public List<PriceAlert> PriceAlerts { get; set; } = new();
}
