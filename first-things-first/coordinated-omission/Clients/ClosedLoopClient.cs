using System.Diagnostics;
using CoordinatedOmission.Histograms;
using CoordinatedOmission.Service;

namespace CoordinatedOmission.Clients;

/// <summary>
/// Closed-loop client: send, wait for response, measure, repeat.
/// Coordinated omission is built in — when the service pauses for 200ms,
/// the client pauses too. No new requests fire during the stall.
///
/// At 450 req/sec with a 200ms pause every 500 requests: 200ms × 450 = 90 requests
/// that SHOULD have been in flight never get sent. The histogram records only the
/// 27 requests that directly hit a pause (~0.2% of 13,500 total). The other 90 per
/// pause event vanish — never measured, never reported.
///
/// Result: p99 stays at ~1ms. The dashboard looks clean because the test stopped
/// asking questions the moment the system stopped answering.
/// Compare with OpenLoopClient: same service, same load, p99 = 195ms.
/// </summary>
public static class ClosedLoopClient
{
    /// <summary>
    /// Sends totalRequests sequentially: start timer → process → stop timer → record.
    /// The sequential loop IS the problem — each iteration waits for the previous one.
    /// During a 200ms pause, the loop stalls and the effective request rate drops to ~5/sec.
    /// </summary>
    public static LatencyReport Run(SimulatedService service, int ratePerSec, int durationSec)
    {
        int totalRequests = ratePerSec * durationSec;
        var recorder = new LatencyRecorder();

        for (int i = 0; i < totalRequests; i++)
        {
            long start = Stopwatch.GetTimestamp();
            service.Process();
            long elapsed = Stopwatch.GetTimestamp() - start;
            recorder.Record(elapsed);
        }

        return recorder.GetReport();
    }
}
