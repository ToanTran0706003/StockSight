using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface IPortfolioService
{
    Task<Portfolio?> GetPortfolioAsync(Guid portfolioId, CancellationToken ct = default);

    Task<PortfolioPosition> BuyAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default);

    Task<PortfolioPosition> SellAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default);
}
