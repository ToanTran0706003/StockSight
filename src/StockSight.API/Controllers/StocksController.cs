using Microsoft.AspNetCore.Mvc;
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

    public StocksController(IStockDataProvider provider, ICacheService cache)
    {
        _provider = provider;
        _cache = cache;
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
}
