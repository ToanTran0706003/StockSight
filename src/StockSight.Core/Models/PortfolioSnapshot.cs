namespace StockSight.Core.Models;

public record PortfolioSnapshot(
    Guid PortfolioId,
    string Name,
    decimal InitialCash,
    decimal CashBalance,
    decimal MarketValue,
    decimal TotalValue,
    decimal TotalPnL,
    decimal TotalPnLPercent,
    IReadOnlyList<PortfolioPositionSnapshot> Positions);

public record PortfolioPositionSnapshot(
    string Symbol,
    decimal Shares,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal PnL,
    decimal PnLPercent);
