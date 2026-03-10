// Standalone runner for perf profiling — no BDN overhead.
// Build: dotnet build -c Release
// Run:   dotnet run -c Release -- perf-sequential 8000000
//        dotnet run -c Release -- perf-random 8000000

namespace HardwareCounters;

public static class PerfRunner
{
    public static void Run(string mode, int n)
    {
        var data = new long[n];
        var rng = new Random(42);

        for (int i = 0; i < n; i++)
            data[i] = rng.NextInt64(1, 1000);

        // Fisher-Yates shuffle
        var indices = new int[n];
        for (int i = 0; i < n; i++)
            indices[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Warmup
        if (mode == "perf-sequential")
        {
            SumSequential(data);
            SumSequential(data);
        }
        else
        {
            SumRandom(data, indices);
            SumRandom(data, indices);
        }

        // Suppress GC during measurement
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.TryStartNoGCRegion(256 * 1024 * 1024L);

        // Hot loop — this is what perf sees
        long result = 0;
        int iterations = mode == "perf-sequential" ? 500 : 50;
        for (int iter = 0; iter < iterations; iter++)
        {
            if (mode == "perf-sequential")
                result += SumSequential(data);
            else
                result += SumRandom(data, indices);
        }

        try { GC.EndNoGCRegion(); } catch { }

        // Prevent DCE
        if (result == 0) Console.Write(" ");
    }

    private static long SumSequential(long[] data)
    {
        long sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    private static long SumRandom(long[] data, int[] indices)
    {
        long sum = 0;
        for (int i = 0; i < indices.Length; i++)
            sum += data[indices[i]];
        return sum;
    }
}
