using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolio;

    public PortfolioController(IPortfolioService portfolio) => _portfolio = portfolio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PortfolioResponse>>> List(CancellationToken ct)
        => Ok((await _portfolio.GetPortfoliosAsync(User.GetUserId(), ct)).Select(ToResponse).ToArray());

    [HttpPost]
    public async Task<ActionResult<PortfolioResponse>> Create([FromBody] CreatePortfolioRequest request, CancellationToken ct)
    {
        var portfolio = await _portfolio.CreatePortfolioAsync(User.GetUserId(), request.Name, request.InitialCash, ct);
        return CreatedAtAction(nameof(Get), new { id = portfolio.Id }, ToResponse(portfolio));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PortfolioResponse>> Get(Guid id, CancellationToken ct)
    {
        var portfolio = await _portfolio.GetPortfolioAsync(id, ct);
        return portfolio is null || portfolio.UserId != User.GetUserId() ? NotFound() : Ok(ToResponse(portfolio));
    }

    [HttpPost("{id:guid}/buy")]
    public async Task<ActionResult<PortfolioPositionResponse>> Buy(Guid id, [FromBody] TradeRequest request, CancellationToken ct)
    {
        if (!await OwnsPortfolioAsync(id, ct))
            return NotFound();

        return Ok(ToResponse(await _portfolio.BuyAsync(id, request.Symbol, request.Shares, request.Price, ct)));
    }

    [HttpPost("{id:guid}/sell")]
    public async Task<ActionResult<PortfolioPositionResponse>> Sell(Guid id, [FromBody] TradeRequest request, CancellationToken ct)
    {
        if (!await OwnsPortfolioAsync(id, ct))
            return NotFound();

        return Ok(ToResponse(await _portfolio.SellAsync(id, request.Symbol, request.Shares, request.Price, ct)));
    }

    [HttpGet("{id:guid}/pnl")]
    public async Task<ActionResult<PortfolioSnapshot>> Pnl(Guid id, CancellationToken ct)
    {
        if (!await OwnsPortfolioAsync(id, ct))
            return NotFound();

        var snapshot = await _portfolio.GetSnapshotAsync(id, ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    private static PortfolioResponse ToResponse(Portfolio portfolio)
        => new(
            portfolio.Id,
            portfolio.Name,
            portfolio.InitialCash,
            portfolio.CashBalance,
            portfolio.CreatedUtc,
            portfolio.Positions.Select(ToResponse).ToArray());

    private static PortfolioPositionResponse ToResponse(PortfolioPosition position)
        => new(position.Id, position.Symbol, position.Shares, position.AverageCost, position.BoughtUtc);

    private async Task<bool> OwnsPortfolioAsync(Guid id, CancellationToken ct)
    {
        var portfolio = await _portfolio.GetPortfolioAsync(id, ct);
        return portfolio?.UserId == User.GetUserId();
    }

    public record CreatePortfolioRequest(string Name, decimal InitialCash);
    public record TradeRequest(string Symbol, decimal Shares, decimal Price);
    public record PortfolioResponse(Guid Id, string Name, decimal InitialCash, decimal CashBalance, DateTime CreatedUtc, IReadOnlyList<PortfolioPositionResponse> Positions);
    public record PortfolioPositionResponse(Guid Id, string Symbol, decimal Shares, decimal AverageCost, DateTime BoughtUtc);
}
