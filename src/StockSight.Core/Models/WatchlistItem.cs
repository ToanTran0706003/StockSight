namespace StockSight.Core.Models;

public class WatchlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
