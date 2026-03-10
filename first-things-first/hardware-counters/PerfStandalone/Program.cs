// NativeAOT standalone runner for perf profiling — clean symbols, no JIT.
// Publish: dotnet publish -c Release
// Run:     ./bin/Release/net9.0/linux-x64/publish/PerfStandalone sequential 8000000
//          ./bin/Release/net9.0/linux-x64/publish/PerfStandalone random 8000000

using System.Runtime.CompilerServices;

string mode = args.Length >= 1 ? args[0] : "sequential";
int n = args.Length >= 2 ? int.Parse(args[1]) : 8_000_000;

var data = new long[n];
var rng = new Random(42);
for (int i = 0; i < n; i++)
    data[i] = rng.NextInt64(1, 1000);

var indices = new int[n];
for (int i = 0; i < n; i++)
    indices[i] = i;
for (int i = n - 1; i > 0; i--)
{
    int j = rng.Next(i + 1);
    (indices[i], indices[j]) = (indices[j], indices[i]);
}

// Warmup
if (mode == "sequential") { SumSequential(data); SumSequential(data); }
else { SumRandom(data, indices); SumRandom(data, indices); }

GC.Collect(2, GCCollectionMode.Aggressive, true, true);

int iterations = mode == "sequential" ? 2000 : 100;
long result = 0;
for (int iter = 0; iter < iterations; iter++)
{
    if (mode == "sequential")
        result += SumSequential(data);
    else
        result += SumRandom(data, indices);
}

if (result == 0) Console.Write(" ");

[MethodImpl(MethodImplOptions.NoInlining)]
static long SumSequential(long[] data)
{
    long sum = 0;
    for (int i = 0; i < data.Length; i++)
        sum += data[i];
    return sum;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static long SumRandom(long[] data, int[] indices)
{
    long sum = 0;
    for (int i = 0; i < indices.Length; i++)
        sum += data[indices[i]];
    return sum;
}
