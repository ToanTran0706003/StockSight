using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure.Data;

namespace StockSight.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly StockSightDbContext _db;
    private readonly ICacheService _cache;
    private readonly IStockDataProvider _provider;

    public HealthController(StockSightDbContext db, ICacheService cache, IStockDataProvider provider)
    {
        _db = db;
        _cache = cache;
        _provider = provider;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get(CancellationToken ct)
    {
        var checks = new Dictionary<string, string>
        {
            ["database"] = await CheckAsync(() => _db.Database.CanConnectAsync(ct)),
            ["redis"] = await CheckAsync(async () =>
            {
                string key = $"health:{Guid.NewGuid():N}";
                await _cache.SetAsync(key, "ok", TimeSpan.FromSeconds(5), ct);
                return await _cache.GetAsync<string>(key, ct) == "ok";
            }),
            ["marketData"] = await CheckAsync(async () => await _provider.GetQuoteAsync("AAPL", ct) is not null)
        };

        string status = checks.Values.All(v => v == "Healthy") ? "Healthy" : "Degraded";
        return Ok(new
        {
            status,
            checks,
            version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            timestampUtc = DateTime.UtcNow
        });
    }

    private static async Task<string> CheckAsync(Func<Task<bool>> check)
    {
        try
        {
            return await check() ? "Healthy" : "Unhealthy";
        }
        catch
        {
            return "Unhealthy";
        }
    }
}
