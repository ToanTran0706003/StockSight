using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSight.Core.Enums;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;
using StockSight.Infrastructure.Data;

namespace StockSight.API.Controllers;

[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly StockSightDbContext _db;
    private readonly IAlertService _alerts;

    public AlertsController(StockSightDbContext db, IAlertService alerts)
    {
        _db = db;
        _alerts = alerts;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PriceAlert>>> List(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _db.PriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedUtc)
            .ToListAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult<PriceAlert>> Create([FromBody] CreateAlertRequest request, CancellationToken ct)
    {
        if (request.TargetPrice <= 0)
            return BadRequest("Target price must be greater than zero.");

        var alert = await _alerts.CreateAlertAsync(new PriceAlert
        {
            UserId = User.GetUserId(),
            Symbol = request.Symbol,
            TargetPrice = request.TargetPrice,
            Direction = request.Direction
        }, ct);
        return CreatedAtAction(nameof(List), alert);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var owns = await _db.PriceAlerts.AnyAsync(a => a.Id == id && a.UserId == userId, ct);
        if (!owns)
            return NotFound();

        await _alerts.DeleteAlertAsync(id, ct);
        return NoContent();
    }

    public record CreateAlertRequest(string Symbol, decimal TargetPrice, AlertCondition Direction);
}
