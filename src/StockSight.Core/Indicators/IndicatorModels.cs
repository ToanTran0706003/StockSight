namespace StockSight.Core.Indicators;

public record IndicatorPoint(DateTime TimestampUtc, decimal? Value);

public record MacdPoint(DateTime TimestampUtc, decimal? Macd, decimal? Signal, decimal? Histogram);

public record BollingerPoint(DateTime TimestampUtc, decimal? Middle, decimal? Upper, decimal? Lower);
