using StockSight.Core.Models;

namespace StockSight.Core.Interfaces;

public interface INewsFeedService
{
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(string symbol, int limit = 10, CancellationToken ct = default);
}
