using System.Diagnostics;
using CoordinatedOmission.Histograms;
using CoordinatedOmission.Service;

namespace CoordinatedOmission.Clients;

/// <summary>
/// Open-loop client: constant-rate scheduling, independent of response time.
/// Measures latency from intended start time, not from when processing actually began.
///
/// At 450 req/sec the schedule is: request 0 at t=0, request 1 at t=2.22ms,
/// request 2 at t=4.44ms, etc. When a 200ms pause hits, the schedule keeps ticking.
/// After the pause clears, ~90 requests are behind schedule. Each one measures from
/// its intended start — request N+45 waited 100ms before it even got to Process(),
/// so its recorded latency includes that queue time.
///
/// Result: p99 explodes to ~195ms. The 90 queued requests per pause event fan out
/// across p90-p99.9, creating the bimodal distribution that closed-loop hides.
/// Same service, same pause, same load — the only difference is where the clock starts.
/// </summary>
public static class OpenLoopClient
{
    /// <summary>
    /// Schedules requests at fixed intervals using Stopwatch ticks. The nextSend cursor
    /// advances unconditionally — it does not wait for the previous request to complete.
    /// After a pause, multiple requests fire back-to-back to catch up, each measuring
    /// from its own intended start time (N+1, N+2, N+3...).
    /// </summary>
    public static LatencyReport Run(SimulatedService service, int ratePerSec, int durationSec)
    {
        var recorder = new LatencyRecorder();
        long intervalTicks = Stopwatch.Frequency / ratePerSec;
        long deadline = Stopwatch.GetTimestamp() + (long)durationSec * Stopwatch.Frequency;
        long nextSend = Stopwatch.GetTimestamp();

        while (Stopwatch.GetTimestamp() < deadline)
        {
            long intendedStart = nextSend;
            nextSend += intervalTicks;

            // Process the request (may block during a pause)
            service.Process();

            // Measure from INTENDED start — not from when processing actually began
            long now = Stopwatch.GetTimestamp();
            long latency = now - intendedStart;
            recorder.Record(latency);

            // Spin-wait until next scheduled slot (if we're ahead of schedule)
            while (Stopwatch.GetTimestamp() < nextSend)
                Thread.SpinWait(10);
        }

        return recorder.GetReport();
    }
}
