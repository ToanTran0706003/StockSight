using StockSight.Core.Models;

namespace StockSight.Core.Indicators;

public static class IndicatorCalculator
{
    public static IReadOnlyList<IndicatorPoint> CalculateSma(IReadOnlyList<OhlcvBar> bars, int period)
    {
        ValidatePeriod(period);
        var result = new List<IndicatorPoint>(bars.Count);
        decimal rollingSum = 0m;

        for (int i = 0; i < bars.Count; i++)
        {
            rollingSum += bars[i].Close;
            if (i >= period)
                rollingSum -= bars[i - period].Close;

            result.Add(new IndicatorPoint(
                bars[i].TimestampUtc,
                i >= period - 1 ? rollingSum / period : null));
        }

        return result;
    }

    public static IReadOnlyList<IndicatorPoint> CalculateEma(IReadOnlyList<OhlcvBar> bars, int period)
    {
        ValidatePeriod(period);
        var result = new List<IndicatorPoint>(bars.Count);
        decimal multiplier = 2m / (period + 1);
        decimal? ema = null;

        for (int i = 0; i < bars.Count; i++)
        {
            if (i == period - 1)
            {
                ema = bars.Take(period).Average(b => b.Close);
            }
            else if (i >= period && ema is not null)
            {
                ema = ((bars[i].Close - ema.Value) * multiplier) + ema.Value;
            }

            result.Add(new IndicatorPoint(bars[i].TimestampUtc, i >= period - 1 ? ema : null));
        }

        return result;
    }

    public static IReadOnlyList<IndicatorPoint> CalculateRsi(IReadOnlyList<OhlcvBar> bars, int period = 14)
    {
        ValidatePeriod(period);
        var result = bars.Select(b => new IndicatorPoint(b.TimestampUtc, null)).ToList();
        if (bars.Count <= period)
            return result;

        decimal gainSum = 0m;
        decimal lossSum = 0m;

        for (int i = 1; i <= period; i++)
        {
            decimal change = bars[i].Close - bars[i - 1].Close;
            if (change >= 0)
                gainSum += change;
            else
                lossSum += Math.Abs(change);
        }

        decimal avgGain = gainSum / period;
        decimal avgLoss = lossSum / period;
        result[period] = result[period] with { Value = ToRsi(avgGain, avgLoss) };

        for (int i = period + 1; i < bars.Count; i++)
        {
            decimal change = bars[i].Close - bars[i - 1].Close;
            decimal gain = change > 0 ? change : 0m;
            decimal loss = change < 0 ? Math.Abs(change) : 0m;

            avgGain = ((avgGain * (period - 1)) + gain) / period;
            avgLoss = ((avgLoss * (period - 1)) + loss) / period;
            result[i] = result[i] with { Value = ToRsi(avgGain, avgLoss) };
        }

        return result;
    }

    public static IReadOnlyList<MacdPoint> CalculateMacd(
        IReadOnlyList<OhlcvBar> bars,
        int fast = 12,
        int slow = 26,
        int signal = 9)
    {
        ValidatePeriod(fast);
        ValidatePeriod(slow);
        ValidatePeriod(signal);
        if (fast >= slow)
            throw new ArgumentException("Fast period must be lower than slow period.", nameof(fast));

        var fastEma = CalculateEma(bars, fast).Select(p => p.Value).ToArray();
        var slowEma = CalculateEma(bars, slow).Select(p => p.Value).ToArray();
        var macdValues = new decimal?[bars.Count];

        for (int i = 0; i < bars.Count; i++)
        {
            if (fastEma[i].HasValue && slowEma[i].HasValue)
                macdValues[i] = fastEma[i]!.Value - slowEma[i]!.Value;
        }

        var signalValues = CalculateNullableEma(macdValues, signal);
        var result = new List<MacdPoint>(bars.Count);

        for (int i = 0; i < bars.Count; i++)
        {
            decimal? histogram = macdValues[i].HasValue && signalValues[i].HasValue
                ? macdValues[i]!.Value - signalValues[i]!.Value
                : null;

            result.Add(new MacdPoint(bars[i].TimestampUtc, macdValues[i], signalValues[i], histogram));
        }

        return result;
    }

    public static IReadOnlyList<BollingerPoint> CalculateBollingerBands(
        IReadOnlyList<OhlcvBar> bars,
        int period = 20,
        decimal stdDev = 2m)
    {
        ValidatePeriod(period);
        if (stdDev <= 0)
            throw new ArgumentOutOfRangeException(nameof(stdDev), "Standard deviation multiplier must be positive.");

        var result = new List<BollingerPoint>(bars.Count);

        for (int i = 0; i < bars.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(new BollingerPoint(bars[i].TimestampUtc, null, null, null));
                continue;
            }

            var window = bars.Skip(i - period + 1).Take(period).Select(b => b.Close).ToArray();
            decimal middle = window.Average();
            decimal variance = window.Sum(v => (v - middle) * (v - middle)) / period;
            decimal deviation = Convert.ToDecimal(Math.Sqrt(Convert.ToDouble(variance)));
            decimal band = deviation * stdDev;

            result.Add(new BollingerPoint(bars[i].TimestampUtc, middle, middle + band, middle - band));
        }

        return result;
    }

    private static decimal?[] CalculateNullableEma(IReadOnlyList<decimal?> values, int period)
    {
        var result = new decimal?[values.Count];
        decimal multiplier = 2m / (period + 1);
        decimal? ema = null;
        int validCount = 0;
        decimal seedSum = 0m;

        for (int i = 0; i < values.Count; i++)
        {
            if (!values[i].HasValue)
                continue;

            validCount++;
            if (validCount <= period)
                seedSum += values[i]!.Value;

            if (validCount == period)
            {
                ema = seedSum / period;
            }
            else if (validCount > period && ema is not null)
            {
                ema = ((values[i]!.Value - ema.Value) * multiplier) + ema.Value;
            }

            result[i] = validCount >= period ? ema : null;
        }

        return result;
    }

    private static decimal ToRsi(decimal avgGain, decimal avgLoss)
    {
        if (avgLoss == 0m)
            return 100m;

        decimal rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    private static void ValidatePeriod(int period)
    {
        if (period <= 0)
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be positive.");
    }
}
