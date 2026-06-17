using StockSight.Core.Enums;

namespace StockSight.Core.Signals;

public record RuleSignalResult(
    SignalAction Action,
    decimal Confidence,
    string Reason,
    decimal BullishScore,
    decimal BearishScore);
