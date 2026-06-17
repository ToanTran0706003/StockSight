using StockSight.Core.Enums;

namespace StockSight.Core.Models;

public class PriceAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal TargetPrice { get; set; }

    public AlertCondition Direction { get; set; } = AlertCondition.Above;

    public bool IsTriggered { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? TriggeredUtc { get; set; }

    public User? User { get; set; }
}
