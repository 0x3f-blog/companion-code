using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace HardwareCounters.Benchmarks;

[SimpleJob(warmupCount: 3, iterationCount: 15)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class AccessPatterns
{
    [Params(1_000_000, 8_000_000, 64_000_000)]
    public int N { get; set; }

    private long[] _data = null!;
    private int[] _indices = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new long[N];
        var rng = new Random(42);

        for (int i = 0; i < N; i++)
            _data[i] = rng.NextInt64(1, 1000);

        // Fisher-Yates shuffle — random permutation of 0..N-1
        _indices = new int[N];
        for (int i = 0; i < N; i++)
            _indices[i] = i;

        for (int i = N - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_indices[i], _indices[j]) = (_indices[j], _indices[i]);
        }
    }

    [Benchmark(Baseline = true)]
    public long SumSequential()
    {
        long sum = 0;
        long[] data = _data;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    [Benchmark]
    public long SumRandom()
    {
        long sum = 0;
        long[] data = _data;
        int[] indices = _indices;
        for (int i = 0; i < indices.Length; i++)
            sum += data[indices[i]];
        return sum;
    }
}
