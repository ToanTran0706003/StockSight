using Microsoft.AspNetCore.Mvc;
using StockSight.Core.Interfaces;
using StockSight.Core.Models;

namespace StockSight.API.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly INewsFeedService _news;

    public NewsController(INewsFeedService news) => _news = news;

    [HttpGet("{symbol}")]
    public async Task<ActionResult<IReadOnlyList<NewsArticle>>> Get(string symbol, CancellationToken ct)
        => Ok(await _news.GetLatestAsync(symbol, 10, ct));
}
