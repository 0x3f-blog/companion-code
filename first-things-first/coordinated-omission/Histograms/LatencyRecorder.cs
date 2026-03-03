using System.Diagnostics;
using HdrHistogram;

namespace CoordinatedOmission.Histograms;

/// <summary>
/// Latency recorder backed by Gil Tene's HdrHistogram — the same data structure
/// designed specifically to capture latency distributions without coordinated omission.
///
/// Records raw Stopwatch ticks (not milliseconds) to avoid floating-point rounding
/// on every sample. Conversion to milliseconds happens once at report time.
/// Stopwatch.Frequency varies by OS (~10 MHz on Windows, ~1 GHz on Linux) —
/// recording ticks and converting at the end keeps sub-microsecond precision
/// regardless of platform.
///
/// HdrHistogram uses compressed buckets with 3 significant digits of precision.
/// At 3 digits, a 1ms value is recorded as 1.00ms (±0.005ms) — sufficient for
/// latency work where the interesting signals are 10-100× differences, not
/// sub-percent precision. Memory cost: ~30KB for the full 1-tick-to-60s range.
/// </summary>
public sealed class LatencyRecorder
{
    private readonly LongHistogram _histogram;

    public LatencyRecorder()
    {
        long maxTicks = Stopwatch.Frequency * 60;
        _histogram = new LongHistogram(maxTicks, 3);
    }

    /// <summary>Record a single latency sample in Stopwatch ticks. Drops non-positive values.</summary>
    public void Record(long stopwatchTicks)
    {
        if (stopwatchTicks > 0)
            _histogram.RecordValue(stopwatchTicks);
    }

    public long TotalCount => _histogram.TotalCount;

    public LatencyReport GetReport()
    {
        return new LatencyReport
        {
            TotalCount = _histogram.TotalCount,
            P50Ms      = TicksToMs(_histogram.GetValueAtPercentile(50)),
            P90Ms      = TicksToMs(_histogram.GetValueAtPercentile(90)),
            P99Ms      = TicksToMs(_histogram.GetValueAtPercentile(99)),
            P999Ms     = TicksToMs(_histogram.GetValueAtPercentile(99.9)),
            MaxMs      = TicksToMs(_histogram.GetMaxValue()),
        };
    }

    private static double TicksToMs(long ticks)
    {
        return (double)ticks / Stopwatch.Frequency * 1000.0;
    }
}

/// <summary>
/// Immutable snapshot of percentile latencies in milliseconds.
/// p50/p90/p99/p99.9/max — the standard set for latency analysis.
/// The gap between p99 closed-loop (~1ms) and p99 open-loop (~195ms) is the
/// entire point of this demo: same system, same failures, 182× difference.
/// </summary>
public sealed class LatencyReport
{
    public long TotalCount { get; init; }
    public double P50Ms { get; init; }
    public double P90Ms { get; init; }
    public double P99Ms { get; init; }
    public double P999Ms { get; init; }
    public double MaxMs { get; init; }

    public void Print(string label)
    {
        Console.WriteLine($"  {"Metric",-12} {label}");
        Console.WriteLine($"  {"------",-12} {"------"}");
        Console.WriteLine($"  {"Count",-12} {TotalCount:N0}");
        Console.WriteLine($"  {"p50",-12} {P50Ms,10:F2} ms");
        Console.WriteLine($"  {"p90",-12} {P90Ms,10:F2} ms");
        Console.WriteLine($"  {"p99",-12} {P99Ms,10:F2} ms");
        Console.WriteLine($"  {"p99.9",-12} {P999Ms,10:F2} ms");
        Console.WriteLine($"  {"max",-12} {MaxMs,10:F2} ms");
    }
}
