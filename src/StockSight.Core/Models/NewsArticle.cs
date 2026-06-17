namespace StockSight.Core.Models;

public record NewsArticle(
    string Symbol,
    string Headline,
    string Source,
    DateTime PublishedUtc,
    string Url,
    string Sentiment,
    decimal Score);
