using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.Infrastructure.AI;

public class NewsFeedService : INewsFeedService
{
    private readonly INewsService _news;

    public NewsFeedService(INewsService news) => _news = news;

    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(string symbol, int limit = 10, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var headlines = await _news.GetNewsBySymbolAsync(symbol, ct);
        var sentiment = await _news.GetSentimentAsync(symbol, ct) ?? 0m;
        var label = sentiment switch
        {
            >= 0.25m => "Bullish",
            <= -0.25m => "Bearish",
            _ => "Neutral"
        };

        var now = DateTime.UtcNow;
        return headlines
            .Take(Math.Clamp(limit, 1, 20))
            .Select((headline, index) => new NewsArticle(
                symbol,
                headline,
                index % 2 == 0 ? "Market Wire" : "Financial Desk",
                now.AddMinutes(-index * 37),
                $"https://news.example.com/{symbol.ToLowerInvariant()}/{index + 1}",
                label,
                decimal.Round(sentiment, 2)))
            .ToArray();
    }
}
