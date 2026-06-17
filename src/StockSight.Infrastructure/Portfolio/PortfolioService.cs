using Microsoft.EntityFrameworkCore;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;

namespace StockSight.Infrastructure.Portfolios;

public class PortfolioService : IPortfolioService
{
    private readonly StockSightDbContext _db;
    private readonly IStockDataProvider _stockData;

    public PortfolioService(StockSightDbContext db, IStockDataProvider stockData)
    {
        _db = db;
        _stockData = stockData;
    }

    public async Task<IReadOnlyList<Core.Models.Portfolio>> GetPortfoliosAsync(Guid userId, CancellationToken ct = default)
        => await _db.Portfolios
            .Include(p => p.Positions)
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<Core.Models.Portfolio?> GetPortfolioAsync(Guid portfolioId, CancellationToken ct = default)
        => await _db.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId, ct);

    public async Task<Core.Models.Portfolio> CreatePortfolioAsync(Guid userId, string name, decimal initialCash, CancellationToken ct = default)
    {
        if (initialCash <= 0)
            throw new InvalidOperationException("Initial cash must be greater than zero.");

        var portfolio = new Core.Models.Portfolio
        {
            UserId = userId,
            OwnerId = userId.ToString(),
            Name = string.IsNullOrWhiteSpace(name) ? "Growth Portfolio" : name.Trim(),
            InitialCash = initialCash,
            CashBalance = initialCash,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync(ct);
        return portfolio;
    }

    public async Task<PortfolioPosition> BuyAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default)
    {
        if (shares <= 0 || price <= 0)
            throw new InvalidOperationException("Shares and price must be greater than zero.");

        symbol = Clean(symbol);
        var portfolio = await GetRequiredPortfolioAsync(portfolioId, ct);
        var cost = shares * price;
        if (portfolio.CashBalance < cost)
            throw new InvalidOperationException("Not enough virtual cash.");

        var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == symbol);
        if (position is null)
        {
            position = new PortfolioPosition
            {
                PortfolioId = portfolioId,
                Symbol = symbol,
                Shares = shares,
                AverageCost = price,
                BoughtUtc = DateTime.UtcNow
            };
            _db.PortfolioPositions.Add(position);
        }
        else
        {
            var totalShares = position.Shares + shares;
            position.AverageCost = ((position.Shares * position.AverageCost) + cost) / totalShares;
            position.Shares = totalShares;
        }

        portfolio.CashBalance -= cost;
        await _db.SaveChangesAsync(ct);
        return position;
    }

    public async Task<PortfolioPosition> SellAsync(Guid portfolioId, string symbol, decimal shares, decimal price, CancellationToken ct = default)
    {
        if (shares <= 0 || price <= 0)
            throw new InvalidOperationException("Shares and price must be greater than zero.");

        symbol = Clean(symbol);
        var portfolio = await GetRequiredPortfolioAsync(portfolioId, ct);
        var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == symbol)
            ?? throw new InvalidOperationException("Position not found.");

        if (position.Shares < shares)
            throw new InvalidOperationException("Not enough shares.");

        position.Shares -= shares;
        portfolio.CashBalance += shares * price;
        if (position.Shares == 0)
            _db.PortfolioPositions.Remove(position);

        await _db.SaveChangesAsync(ct);
        return position;
    }

    public async Task<PortfolioSnapshot?> GetSnapshotAsync(Guid portfolioId, CancellationToken ct = default)
    {
        var portfolio = await GetPortfolioAsync(portfolioId, ct);
        if (portfolio is null)
            return null;

        var positions = new List<PortfolioPositionSnapshot>();
        foreach (var position in portfolio.Positions.OrderBy(p => p.Symbol))
        {
            var quote = await _stockData.GetQuoteAsync(position.Symbol, ct);
            var currentPrice = quote?.Price ?? position.AverageCost;
            var marketValue = position.Shares * currentPrice;
            var pnl = (currentPrice - position.AverageCost) * position.Shares;
            var basis = position.AverageCost * position.Shares;
            positions.Add(new PortfolioPositionSnapshot(
                position.Symbol,
                position.Shares,
                position.AverageCost,
                currentPrice,
                marketValue,
                pnl,
                basis == 0 ? 0 : pnl / basis * 100m));
        }

        var market = positions.Sum(p => p.MarketValue);
        var total = portfolio.CashBalance + market;
        var pnlTotal = total - portfolio.InitialCash;
        var pnlPercent = portfolio.InitialCash == 0 ? 0 : pnlTotal / portfolio.InitialCash * 100m;

        return new PortfolioSnapshot(
            portfolio.Id,
            portfolio.Name,
            portfolio.InitialCash,
            portfolio.CashBalance,
            market,
            total,
            pnlTotal,
            pnlPercent,
            positions);
    }

    private async Task<Core.Models.Portfolio> GetRequiredPortfolioAsync(Guid portfolioId, CancellationToken ct)
        => await _db.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId, ct)
            ?? throw new InvalidOperationException("Portfolio not found.");

    private static string Clean(string symbol) => symbol.Trim().ToUpperInvariant();
}
