using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.Controllers;

[ApiController]
[Authorize]
[Route("api/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlist;

    public WatchlistController(IWatchlistService watchlist) => _watchlist = watchlist;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WatchlistItem>>> Get(CancellationToken ct)
        => Ok(await _watchlist.GetAsync(User.GetUserId(), ct));

    [HttpPost("{symbol}")]
    public async Task<ActionResult<WatchlistItem>> Add(string symbol, CancellationToken ct)
    {
        var item = await _watchlist.AddAsync(User.GetUserId(), symbol, ct);
        return CreatedAtAction(nameof(Get), item);
    }

    [HttpDelete("{symbol}")]
    public async Task<IActionResult> Remove(string symbol, CancellationToken ct)
    {
        var removed = await _watchlist.RemoveAsync(User.GetUserId(), symbol, ct);
        return removed ? NoContent() : NotFound();
    }
}
