namespace StockSight.API.BackgroundServices;

public class StockIngestionOptions
{
    public const string SectionName = "StockIngestion";

    public bool Enabled { get; set; } = true;

    public string[] Symbols { get; set; } = ["AAPL", "MSFT", "GOOGL", "TSLA", "AMZN"];

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}
