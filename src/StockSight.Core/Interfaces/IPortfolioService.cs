using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface IPortfolioService
{
    Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(Guid userId, CancellationToken ct = default);

    Task<Portfolio?> GetPortfolioAsync(Guid portfolioId, CancellationToken ct = default);

    Task<Portfolio> CreatePortfolioAsync(Guid userId, string name, decimal initialCash, CancellationToken ct = default);

    Task<PortfolioPosition> BuyAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default);

    Task<PortfolioPosition> SellAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default);

    Task<PortfolioSnapshot?> GetSnapshotAsync(Guid portfolioId, CancellationToken ct = default);
}
