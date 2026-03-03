using System.Diagnostics;

namespace CoordinatedOmission.Service;

/// <summary>
/// In-process simulated service with deterministic failure injection.
/// Baseline latency ~1ms via calibrated SpinWait (not Thread.Sleep — Sleep has
/// 15ms granularity on Windows and ~1-4ms jitter on Linux, which would drown
/// the signal). SpinWait burns CPU but produces tight, repeatable ~1ms baselines.
///
/// Every N-th request introduces a 200ms Thread.Sleep pause — simulating GC stop-the-world,
/// storage compaction, or network partition. The pause is long enough to be unambiguous
/// in histograms but short enough that the demo runs in ~60 seconds.
///
/// The combination (1ms baseline + 200ms pause every 500 requests) creates the exact
/// conditions where coordinated omission matters: a service that's fast 99.6% of the time
/// but catastrophic for 0.4%. Closed-loop hides it. Open-loop exposes it.
/// </summary>
public sealed class SimulatedService
{
    private readonly int _pauseEveryN;
    private readonly int _pauseMs;
    private readonly int _spinIterations;
    private int _counter;

    public SimulatedService(int pauseEveryN, int pauseMs, int spinIterations)
    {
        _pauseEveryN = pauseEveryN;
        _pauseMs = pauseMs;
        _spinIterations = spinIterations;
    }

    /// <summary>
    /// Process a request. Every N-th call sleeps for pauseMs (the failure).
    /// All other calls spin for ~1ms (the baseline). Interlocked.Increment
    /// ensures the counter is thread-safe if callers ever go concurrent.
    /// </summary>
    public void Process()
    {
        int count = Interlocked.Increment(ref _counter);
        if (count % _pauseEveryN == 0)
        {
            Thread.Sleep(_pauseMs);
        }
        else
        {
            Thread.SpinWait(_spinIterations);
        }
    }

    /// <summary>
    /// Binary search for SpinWait iteration count that produces ~targetMs on current hardware.
    /// Different CPUs need vastly different iteration counts: ~150K on Xeon E5-2697 v2,
    /// ~300K on Ryzen 7950X, ~80K on Apple M2. Without calibration the baseline latency
    /// would vary 5-20× across machines, making the demo results hardware-dependent.
    /// Uses median of 50 samples (after warmup) to ignore OS scheduling outliers.
    /// </summary>
    public static int CalibrateSpinWait(int targetMs = 1, int samples = 50)
    {
        int lo = 1_000;
        int hi = 50_000_000;

        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            double median = MeasureMedianMs(mid, samples);

            if (median < targetMs)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo;
    }

    private static double MeasureMedianMs(int iterations, int samples)
    {
        var timings = new double[samples];
        // Warmup
        for (int i = 0; i < 5; i++)
            Thread.SpinWait(iterations);

        for (int i = 0; i < samples; i++)
        {
            long start = Stopwatch.GetTimestamp();
            Thread.SpinWait(iterations);
            long elapsed = Stopwatch.GetTimestamp() - start;
            timings[i] = (double)elapsed / Stopwatch.Frequency * 1000.0;
        }

        Array.Sort(timings);
        return timings[samples / 2];
    }
}
