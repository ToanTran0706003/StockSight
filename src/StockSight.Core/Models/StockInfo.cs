namespace StockSight.Core.Models;

public class StockInfo
{
    public string Symbol { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Exchange { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public decimal? MarketCap { get; set; }

    public decimal? PeRatio { get; set; }

    public decimal? DividendYield { get; set; }
}
