using System.Diagnostics;
using CoordinatedOmission.Clients;
using CoordinatedOmission.Service;

// --- Parse CLI arguments ---
int rate = GetArg(args, "--rate", 450);
int duration = GetArg(args, "--duration", 30);
int pauseEvery = GetArg(args, "--pause-every", 500);
int pauseMs = GetArg(args, "--pause-ms", 200);

Console.WriteLine("=== Coordinated Omission Demo ===");
Console.WriteLine();
Console.WriteLine($"  Rate:        {rate} req/sec");
Console.WriteLine($"  Duration:    {duration} sec");
Console.WriteLine($"  Pause every: {pauseEvery} requests");
Console.WriteLine($"  Pause:       {pauseMs} ms");
Console.WriteLine($"  Stopwatch:   {Stopwatch.Frequency / 1_000_000.0:F1} MHz");
Console.WriteLine();

// --- Calibrate SpinWait for ~1ms on this hardware ---
Console.Write("Calibrating SpinWait for ~1ms baseline... ");
int spinIterations = SimulatedService.CalibrateSpinWait(targetMs: 1, samples: 50);
Console.WriteLine($"done ({spinIterations:N0} iterations).");
Console.WriteLine();

// --- Run closed-loop ---
Console.WriteLine("--- Closed-loop client ---");
Console.WriteLine("  (send → wait → measure → repeat)");
Console.WriteLine();
var closedService = new SimulatedService(pauseEvery, pauseMs, spinIterations);
var closedReport = ClosedLoopClient.Run(closedService, rate, duration);
closedReport.Print("Closed-loop");
Console.WriteLine();

// --- Run open-loop ---
Console.WriteLine("--- Open-loop client ---");
Console.WriteLine("  (constant rate, measure from intended start)");
Console.WriteLine();
var openService = new SimulatedService(pauseEvery, pauseMs, spinIterations);
var openReport = OpenLoopClient.Run(openService, rate, duration);
openReport.Print("Open-loop");
Console.WriteLine();

// --- Side-by-side comparison ---
Console.WriteLine("=== Side-by-side comparison ===");
Console.WriteLine();
Console.WriteLine($"  {"Metric",-12} {"Closed-loop",14} {"Open-loop",14} {"Ratio",10}");
Console.WriteLine($"  {"------",-12} {"-----------",14} {"---------",14} {"-----",10}");
PrintRow("Count", closedReport.TotalCount, openReport.TotalCount);
PrintRowMs("p50", closedReport.P50Ms, openReport.P50Ms);
PrintRowMs("p90", closedReport.P90Ms, openReport.P90Ms);
PrintRowMs("p99", closedReport.P99Ms, openReport.P99Ms);
PrintRowMs("p99.9", closedReport.P999Ms, openReport.P999Ms);
PrintRowMs("max", closedReport.MaxMs, openReport.MaxMs);
Console.WriteLine();

static void PrintRow(string label, long a, long b)
{
    Console.WriteLine($"  {label,-12} {a,14:N0} {b,14:N0} {"",10}");
}

static void PrintRowMs(string label, double a, double b)
{
    string ratio = a > 0 ? $"{b / a:F1}x" : "-";
    Console.WriteLine($"  {label,-12} {a,11:F2} ms {b,11:F2} ms {ratio,10}");
}

static int GetArg(string[] args, string name, int defaultValue)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name && int.TryParse(args[i + 1], out int value))
            return value;
    }
    return defaultValue;
}
