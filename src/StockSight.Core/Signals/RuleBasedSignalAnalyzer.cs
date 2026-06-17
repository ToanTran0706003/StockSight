using StockSight.Core.Enums;
using StockSight.Core.Indicators;
using StockSight.Core.Models;

namespace StockSight.Core.Signals;

public class RuleBasedSignalAnalyzer
{
    public RuleSignalResult Analyze(IReadOnlyList<OhlcvBar> bars)
    {
        if (bars.Count < 35)
            return new RuleSignalResult(SignalAction.Hold, 35m, "Not enough candle history for a reliable signal.", 0m, 0m);

        var rsi = IndicatorCalculator.CalculateRsi(bars, 14);
        var macd = IndicatorCalculator.CalculateMacd(bars);
        var bollinger = IndicatorCalculator.CalculateBollingerBands(bars);

        decimal bullish = 0m;
        decimal bearish = 0m;
        var reasons = new List<string>();

        var currentRsi = rsi.LastOrDefault(p => p.Value.HasValue)?.Value;
        if (currentRsi < 30m)
        {
            bullish += 35m;
            reasons.Add($"RSI oversold ({currentRsi:N1})");
        }
        else if (currentRsi > 70m)
        {
            bearish += 35m;
            reasons.Add($"RSI overbought ({currentRsi:N1})");
        }

        var previousMacd = macd.Take(macd.Count - 1).LastOrDefault(p => p.Macd.HasValue && p.Signal.HasValue);
        var currentMacd = macd.LastOrDefault(p => p.Macd.HasValue && p.Signal.HasValue);
        if (previousMacd is not null && currentMacd is not null)
        {
            bool crossedUp = previousMacd.Macd <= previousMacd.Signal && currentMacd.Macd > currentMacd.Signal;
            bool crossedDown = previousMacd.Macd >= previousMacd.Signal && currentMacd.Macd < currentMacd.Signal;

            if (crossedUp)
            {
                bullish += 30m;
                reasons.Add("MACD bullish crossover");
            }
            else if (crossedDown)
            {
                bearish += 30m;
                reasons.Add("MACD bearish crossover");
            }
        }

        var latest = bars[^1];
        var currentBand = bollinger.LastOrDefault(p => p.Middle.HasValue);
        if (currentBand is not null)
        {
            if (latest.Close <= currentBand.Lower)
            {
                bullish += 25m;
                reasons.Add("Price touched lower Bollinger Band");
            }
            else if (latest.Close >= currentBand.Upper)
            {
                bearish += 25m;
                reasons.Add("Price touched upper Bollinger Band");
            }
        }

        decimal net = bullish - bearish;
        SignalAction action = net switch
        {
            >= 20m => SignalAction.Buy,
            <= -20m => SignalAction.Sell,
            _ => SignalAction.Hold
        };

        decimal confidence = action == SignalAction.Hold
            ? 50m - Math.Min(20m, Math.Abs(net) / 2m)
            : Math.Min(95m, 50m + Math.Abs(net));

        string reason = reasons.Count == 0
            ? "No strong technical trigger; indicators are mixed."
            : string.Join("; ", reasons);

        return new RuleSignalResult(action, decimal.Round(confidence, 2), reason, bullish, bearish);
    }
}
