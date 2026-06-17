using Microsoft.AspNetCore.Mvc;
using StockSight.Core.Backtesting;
using StockSight.Core.Backtesting.Strategies;
using StockSight.Core.Interfaces;

namespace StockSight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly IStockDataProvider _provider;
    private readonly BacktestEngine _engine = new();

    public BacktestController(IStockDataProvider provider)
    {
        _provider = provider;
    }

    [HttpPost]
    public async Task<ActionResult<BacktestResult>> Run([FromBody] BacktestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("Symbol is required.");

        if (request.InitialCapital <= 0)
            return BadRequest("Initial capital must be positive.");

        DateTime to = request.To == default ? DateTime.UtcNow : request.To.ToUniversalTime();
        DateTime from = request.From == default ? to.AddYears(-1) : request.From.ToUniversalTime();
        if (from >= to)
            return BadRequest("From date must be earlier than To date.");

        var bars = await _provider.GetOhlcvAsync(request.Symbol, "1d", from, to, ct);
        if (bars.Count < 60)
            return BadRequest("Backtest requires at least 60 candles.");

        var strategy = CreateStrategy(request.Strategy);
        return Ok(_engine.Run(strategy, request.Symbol, bars, request.InitialCapital));
    }

    private static IBacktestStrategy CreateStrategy(string? strategy)
        => strategy?.Trim().ToLowerInvariant() switch
        {
            "rsireversal" or "rsi" => new RsiReversalStrategy(),
            "macd" => new MacdStrategy(),
            _ => new SmaCrossoverStrategy()
        };
}
