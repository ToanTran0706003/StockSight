namespace StockSight.Core.Enums;

/// <summary>The direction an alert fires in relative to its threshold price.</summary>
public enum AlertCondition
{
    /// <summary>Fire when the last price rises to or above <c>TargetPrice</c>.</summary>
    Above,

    /// <summary>Fire when the last price falls to or below <c>TargetPrice</c>.</summary>
    Below
}

/// <summary>Lifecycle state of an <see cref="Models.Alert"/>.</summary>
public enum AlertStatus
{
    Active,
    Triggered,
    Disabled
}
