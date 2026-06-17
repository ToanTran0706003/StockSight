using StockSight.Core.Enums;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Core.Signals;

namespace StockSight.Infrastructure.AI;

public class SignalEngine : ISignalEngine
{
    private readonly IStockDataProvider _provider;
    private readonly INewsService _news;
    private readonly ICacheService _cache;
    private readonly RuleBasedSignalAnalyzer _rules = new();

    public SignalEngine(IStockDataProvider provider, INewsService news, ICacheService cache)
    {
        _provider = provider;
        _news = news;
        _cache = cache;
    }

    public async Task<TradeSignal> AnalyzeAsync(string symbol, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        string cacheKey = $"signal:{symbol}";
        var cached = await _cache.GetAsync<TradeSignal>(cacheKey, ct);
        if (cached is not null)
            return cached;

        DateTime toUtc = DateTime.UtcNow;
        var bars = await _provider.GetOhlcvAsync(symbol, "1d", toUtc.AddDays(-180), toUtc, ct);
        var rule = _rules.Analyze(bars);
        decimal sentiment = await _news.GetSentimentAsync(symbol, ct) ?? 0m;

        decimal adjustedNet = (rule.BullishScore - rule.BearishScore) + (sentiment * 20m);
        SignalAction action = adjustedNet switch
        {
            >= 20m => SignalAction.Buy,
            <= -20m => SignalAction.Sell,
            _ => SignalAction.Hold
        };

        decimal confidence = action == SignalAction.Hold
            ? Math.Max(35m, rule.Confidence - Math.Abs(sentiment) * 8m)
            : Math.Min(98m, rule.Confidence + Math.Abs(sentiment) * 12m);

        var signal = new TradeSignal
        {
            Symbol = symbol,
            Action = action,
            Confidence = decimal.Round(confidence, 2),
            Reason = $"{rule.Reason.TrimEnd('.')}. News sentiment score: {sentiment:N2}.",
            SentimentScore = decimal.Round(sentiment, 2),
            GeneratedUtc = DateTime.UtcNow
        };

        await _cache.SetAsync(cacheKey, signal, TimeSpan.FromMinutes(15), ct);
        return signal;
    }
}
