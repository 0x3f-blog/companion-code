using BenchmarkDotNet.Attributes;

namespace StatisticsThatMatter.Benchmarks;

/// <summary>
/// Layer 2 — Effect size: when "significant" doesn't mean "meaningful."
/// Two pairs demonstrating the gap between statistical and practical significance.
///
/// SmallEffect (SumArray vs SumSpan): Ratio ~1.01, Cohen's d = 1.98 ("large").
/// The JIT produces near-identical code for both. The 0.5% difference is
/// statistically real (CIs don't overlap) but not worth a code change. Yet d says
/// "large." Cohen's thresholds (0.2/0.5/0.8) were calibrated for psychology
/// experiments with naturally high within-group variance. They do not apply to
/// microbenchmarks. Use BDN's Ratio column instead.
///
/// LargeEffect (SearchLinear vs SearchBinary): Ratio ~0.001, Cohen's d = 368.
/// O(n) vs O(log n) — an algorithmic change, not a JIT quirk. 1,071× faster,
/// reproducible on any hardware with sorted data. Both d values are "large"
/// by Cohen's thresholds. Only one is a meaningful optimization.
/// </summary>
[SimpleJob(warmupCount: 5, iterationCount: 20)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EffectSizeDemo
{
    [Params(1_000_000)]
    public int N { get; set; }

    private int[] _data = null!;
    private int[] _sorted = null!;
    private int[] _searchKeys = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _data = new int[N];
        for (int i = 0; i < N; i++)
            _data[i] = rng.Next(0, N * 10);

        _sorted = (int[])_data.Clone();
        Array.Sort(_sorted);

        // 1000 search keys — mix of hits and misses
        _searchKeys = new int[1000];
        for (int i = 0; i < _searchKeys.Length; i++)
            _searchKeys[i] = rng.Next(0, N * 10);
    }

    // --- Small effect: array indexing vs Span indexing ---

    /// <summary>
    /// Sum via array indexing. The JIT applies bounds-check elimination for
    /// `for (int i = 0; i &lt; data.Length; i++) data[i]` on .NET 9 — the loop
    /// variable is provably within bounds. Produces nearly identical machine
    /// code to the Span variant. Ratio ~1.01 — the 2.6 µs difference (0.5%)
    /// is statistically real but not worth a code change.
    /// </summary>
    [BenchmarkCategory("SmallEffect")]
    [Benchmark(Baseline = true)]
    public long SumArray()
    {
        long sum = 0;
        int[] data = _data;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    /// <summary>
    /// Sum via ReadOnlySpan indexing. Bounds-check elimination applies here too —
    /// the JIT recognizes the same pattern on Span. The generated assembly is
    /// near-identical to SumArray. Cohen's d = 1.98 ("large") for a 0.5% difference —
    /// a textbook example of why d misleads in microbenchmarks.
    /// </summary>
    [BenchmarkCategory("SmallEffect")]
    [Benchmark]
    public long SumSpan()
    {
        long sum = 0;
        ReadOnlySpan<int> span = _data;
        for (int i = 0; i < span.Length; i++)
            sum += span[i];
        return sum;
    }

    // --- Large effect: linear search vs binary search ---

    /// <summary>
    /// Linear search via Array.IndexOf over 1000 search keys on 1M sorted integers.
    /// O(n) per search × 1000 keys. Each IndexOf scans up to 1M elements sequentially.
    /// Mean ~248 ms — dominated by memory bandwidth on sequential scan. 1000 keys.
    /// </summary>
    [BenchmarkCategory("LargeEffect")]
    [Benchmark(Baseline = true)]
    public int SearchLinear()
    {
        int found = 0;
        var sorted = _sorted;
        var keys = _searchKeys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (Array.IndexOf(sorted, keys[i]) >= 0)
                found++;
        }
        return found;
    }

    /// <summary>
    /// Binary search via Array.BinarySearch over 1000 search keys on 1M sorted integers.
    /// O(log n) per search × 1000 keys. Each BinarySearch touches ~20 elements.
    /// Mean ~232 µs — 1,071× faster than linear. This is an algorithmic difference:
    /// O(n) vs O(log n). Ratio = 0.001. The kind of difference worth shipping.
    /// </summary>
    [BenchmarkCategory("LargeEffect")]
    [Benchmark]
    public int SearchBinary()
    {
        int found = 0;
        var sorted = _sorted;
        var keys = _searchKeys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (Array.BinarySearch(sorted, keys[i]) >= 0)
                found++;
        }
        return found;
    }
}
