using StockSight.Core.Enums;

namespace StockSight.Core.Models;

/// <summary>
/// A user-defined price alert evaluated against incoming <see cref="StockTick"/>s.
/// </summary>
public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OwnerId { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public decimal TargetPrice { get; set; }

    public AlertCondition Condition { get; set; } = AlertCondition.Above;

    public AlertStatus Status { get; set; } = AlertStatus.Active;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? TriggeredUtc { get; set; }

    /// <summary>Returns true when <paramref name="price"/> satisfies the alert condition.</summary>
    public bool IsMet(decimal price) => Condition switch
    {
        AlertCondition.Above => price >= TargetPrice,
        AlertCondition.Below => price <= TargetPrice,
        _ => false
    };
}
