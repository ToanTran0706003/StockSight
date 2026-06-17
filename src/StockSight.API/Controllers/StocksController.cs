using Microsoft.AspNetCore.Mvc;
using StockSight.Core.Indicators;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.Controllers;

/// <summary>
/// Sample read endpoint that demonstrates the cache-aside pattern:
/// serve from Redis, fall back to the market-data provider, then cache.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockDataProvider _provider;
    private readonly ICacheService _cache;
    private readonly ISignalEngine? _signalEngine;

    public StocksController(IStockDataProvider provider, ICacheService cache, ISignalEngine? signalEngine = null)
    {
        _provider = provider;
        _cache = cache;
        _signalEngine = signalEngine;
    }

    /// <summary>GET /api/stocks/{symbol} — latest quote (cached ~30s).</summary>
    [HttpGet("{symbol}")]
    public Task<ActionResult<StockTick>> GetQuoteLegacy(string symbol, CancellationToken ct)
        => GetQuote(symbol, ct);

    /// <summary>GET /api/stocks/{symbol}/quote — latest quote (cached ~30s).</summary>
    [HttpGet("{symbol}/quote")]
    public async Task<ActionResult<StockTick>> GetQuote(string symbol, CancellationToken ct)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        string cacheKey = $"quote:{symbol}";

        var cached = await _cache.GetAsync<StockTick>(cacheKey, ct);
        if (cached is not null)
            return Ok(cached);

        var tick = await _provider.GetQuoteAsync(symbol, ct);
        if (tick is null)
            return NotFound($"No quote available for '{symbol}'.");

        await _cache.SetAsync(cacheKey, tick, ct: ct);
        return Ok(tick);
    }

    [HttpGet("{symbol}/ohlcv")]
    public async Task<ActionResult<object>> GetOhlcv(
        string symbol,
        [FromQuery] string interval = "1d",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        DateTime toUtc = (to ?? DateTime.UtcNow).ToUniversalTime();
        DateTime fromUtc = (from ?? toUtc.AddDays(-30)).ToUniversalTime();
        string cacheKey = $"ohlcv:{symbol}:{interval}:{fromUtc:yyyyMMdd}:{toUtc:yyyyMMdd}";

        var cached = await _cache.GetAsync<IReadOnlyList<OhlcvBar>>(cacheKey, ct);
        if (cached is not null)
            return Ok(new { symbol, interval, bars = cached });

        var bars = await _provider.GetOhlcvAsync(symbol, interval, fromUtc, toUtc, ct);
        await _cache.SetAsync(cacheKey, bars, TimeSpan.FromMinutes(5), ct);
        return Ok(new { symbol, interval, bars });
    }

    [HttpGet("{symbol}/info")]
    public async Task<ActionResult<StockInfo>> GetInfo(string symbol, CancellationToken ct)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        string cacheKey = $"info:{symbol}";

        var cached = await _cache.GetAsync<StockInfo>(cacheKey, ct);
        if (cached is not null)
            return Ok(cached);

        var info = await _provider.GetStockInfoAsync(symbol, ct);
        if (info is null)
            return NotFound($"No stock info available for '{symbol}'.");

        await _cache.SetAsync(cacheKey, info, TimeSpan.FromHours(24), ct);
        return Ok(info);
    }

    [HttpGet("search")]
    public async Task<ActionResult<object>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var results = await _provider.SearchSymbolsAsync(q, ct);
        return Ok(new { results });
    }

    [HttpGet("{symbol}/indicators/sma")]
    public async Task<ActionResult<object>> GetSma(
        string symbol,
        [FromQuery] int period = 20,
        [FromQuery] string interval = "1d",
        CancellationToken ct = default)
    {
        var bars = await GetIndicatorBarsAsync(symbol, interval, period, ct);
        var values = IndicatorCalculator.CalculateSma(bars, period);
        return Ok(new
        {
            symbol = symbol.Trim().ToUpperInvariant(),
            indicator = "SMA",
            period,
            values,
            currentValue = values.LastOrDefault(p => p.Value.HasValue)?.Value
        });
    }

    [HttpGet("{symbol}/indicators/ema")]
    public async Task<ActionResult<object>> GetEma(
        string symbol,
        [FromQuery] int period = 50,
        [FromQuery] string interval = "1d",
        CancellationToken ct = default)
    {
        var bars = await GetIndicatorBarsAsync(symbol, interval, period, ct);
        var values = IndicatorCalculator.CalculateEma(bars, period);
        return Ok(new
        {
            symbol = symbol.Trim().ToUpperInvariant(),
            indicator = "EMA",
            period,
            values,
            currentValue = values.LastOrDefault(p => p.Value.HasValue)?.Value
        });
    }

    [HttpGet("{symbol}/indicators/rsi")]
    public async Task<ActionResult<object>> GetRsi(
        string symbol,
        [FromQuery] int period = 14,
        [FromQuery] string interval = "1d",
        CancellationToken ct = default)
    {
        var bars = await GetIndicatorBarsAsync(symbol, interval, period + 1, ct);
        var values = IndicatorCalculator.CalculateRsi(bars, period);
        var currentValue = values.LastOrDefault(p => p.Value.HasValue)?.Value;
        return Ok(new
        {
            symbol = symbol.Trim().ToUpperInvariant(),
            indicator = "RSI",
            period,
            values,
            currentValue,
            signal = currentValue switch
            {
                < 30m => "Oversold",
                > 70m => "Overbought",
                _ => "Neutral"
            }
        });
    }

    [HttpGet("{symbol}/indicators/macd")]
    public async Task<ActionResult<object>> GetMacd(
        string symbol,
        [FromQuery] int fast = 12,
        [FromQuery] int slow = 26,
        [FromQuery] int signal = 9,
        [FromQuery] string interval = "1d",
        CancellationToken ct = default)
    {
        var bars = await GetIndicatorBarsAsync(symbol, interval, slow + signal, ct);
        var values = IndicatorCalculator.CalculateMacd(bars, fast, slow, signal);
        var current = values.LastOrDefault(p => p.Macd.HasValue && p.Signal.HasValue);
        return Ok(new
        {
            symbol = symbol.Trim().ToUpperInvariant(),
            indicator = "MACD",
            fast,
            slow,
            signal,
            values,
            currentMacd = current?.Macd,
            currentSignal = current?.Signal,
            currentHistogram = current?.Histogram
        });
    }

    [HttpGet("{symbol}/indicators/bollinger")]
    public async Task<ActionResult<object>> GetBollinger(
        string symbol,
        [FromQuery] int period = 20,
        [FromQuery] decimal stdDev = 2m,
        [FromQuery] string interval = "1d",
        CancellationToken ct = default)
    {
        var bars = await GetIndicatorBarsAsync(symbol, interval, period, ct);
        var values = IndicatorCalculator.CalculateBollingerBands(bars, period, stdDev);
        var current = values.LastOrDefault(p => p.Middle.HasValue);
        return Ok(new
        {
            symbol = symbol.Trim().ToUpperInvariant(),
            indicator = "Bollinger",
            period,
            stdDev,
            values,
            currentMiddle = current?.Middle,
            currentUpper = current?.Upper,
            currentLower = current?.Lower
        });
    }

    [HttpGet("{symbol}/signal")]
    public async Task<ActionResult<TradeSignal>> GetSignal(string symbol, CancellationToken ct)
    {
        if (_signalEngine is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Signal engine is not available.");

        var signal = await _signalEngine.AnalyzeAsync(symbol, ct);
        return Ok(signal);
    }

    private async Task<IReadOnlyList<OhlcvBar>> GetIndicatorBarsAsync(
        string symbol,
        string interval,
        int minimumPeriod,
        CancellationToken ct)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        DateTime toUtc = DateTime.UtcNow;
        DateTime fromUtc = interval.Equals("1d", StringComparison.OrdinalIgnoreCase) ||
                           interval.Equals("1w", StringComparison.OrdinalIgnoreCase)
            ? toUtc.AddDays(-Math.Max(180, minimumPeriod * 4))
            : toUtc.AddDays(-30);

        string cacheKey = $"ohlcv:{symbol}:{interval}:indicators";
        var cached = await _cache.GetAsync<IReadOnlyList<OhlcvBar>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var bars = await _provider.GetOhlcvAsync(symbol, interval, fromUtc, toUtc, ct);
        await _cache.SetAsync(cacheKey, bars, TimeSpan.FromMinutes(5), ct);
        return bars;
    }
}
