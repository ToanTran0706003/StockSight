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
}
