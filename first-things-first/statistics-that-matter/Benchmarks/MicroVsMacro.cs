using BenchmarkDotNet.Attributes;

namespace StatisticsThatMatter.Benchmarks;

/// <summary>
/// Layer 3 — Micro vs macro: right question, wrong scale.
/// A microbenchmark isolates a function. A macrobenchmark places it inside a pipeline.
/// They answer different questions.
///
/// Micro: Dictionary lookup is 262× faster than linear search over 10K elements.
/// Macro: the same optimization improves the full pipeline by ~8%.
///
/// Amdahl's law explains the gap. The lookup consumes f = 0.057 (5.7%) of the pipeline.
/// With speedup S = 262: max improvement = 1 / (1 − f + f/S) = 1 / (1 − 0.057 + 0.057/262)
/// = 1 / 0.9432 ≈ 6.0%. The measured 8% is higher — cache effects from
/// eliminating the linear scan likely benefit subsequent pipeline steps.
///
/// Pipeline proportions (~approximate):
/// - ValidateArray:       ~40% — sequential scan and checksum, O(n)
/// - PolynomialTransform: ~40% — multiply/add computation, O(n)
/// - Lookup:               ~6% — 200 searches over 10K elements (linear or dictionary)
/// - Aggregate:           ~15% — running weighted sum, O(n) with stride 4
///
/// Micro says "262× faster." Macro says "8% faster." The user says "I don't see a difference."
/// </summary>
[SimpleJob(warmupCount: 5, iterationCount: 20)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MicroVsMacro
{
    private int[] _data = null!;
    private int[] _searchKeys = null!;
    private Dictionary<int, int> _dict = null!;
    private int[] _workload = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        // 10,000 element dataset for lookups
        _data = new int[10_000];
        for (int i = 0; i < _data.Length; i++)
            _data[i] = rng.Next(0, 100_000);

        // Build dictionary from the same data
        _dict = new Dictionary<int, int>(_data.Length);
        for (int i = 0; i < _data.Length; i++)
            _dict[_data[i]] = i;

        // 200 keys to look up per pipeline run
        _searchKeys = new int[200];
        for (int i = 0; i < _searchKeys.Length; i++)
            _searchKeys[i] = rng.Next(0, 100_000);

        // 3M element workload array for the pipeline's non-lookup steps
        _workload = new int[3_000_000];
        for (int i = 0; i < _workload.Length; i++)
            _workload[i] = rng.Next(0, 100_000);
    }

    // --- Micro: isolated lookup comparison ---

    /// <summary>
    /// Linear search over 10K elements, 200 search keys.
    /// Array.IndexOf scans sequentially — O(n) per key, O(n×k) total.
    /// Mean ~411 µs. This is the baseline that makes Dictionary look 262× faster.
    /// </summary>
    [BenchmarkCategory("Micro")]
    [Benchmark(Baseline = true)]
    public int LookupLinear()
    {
        int found = 0;
        var data = _data;
        var keys = _searchKeys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (Array.IndexOf(data, keys[i]) >= 0)
                found++;
        }
        return found;
    }

    /// <summary>
    /// Dictionary lookup over 10K elements, 200 search keys.
    /// Hash-based O(1) per key, O(k) total. Mean ~1.6 µs.
    /// Ratio ~0.004 — 262× faster in isolation. But isolation is the key word.
    /// </summary>
    [BenchmarkCategory("Micro")]
    [Benchmark]
    public int LookupDictionary()
    {
        int found = 0;
        var dict = _dict;
        var keys = _searchKeys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (dict.ContainsKey(keys[i]))
                found++;
        }
        return found;
    }

    // --- Macro: full pipeline where lookup is ~6% of work ---

    /// <summary>
    /// Full pipeline with linear lookup. Validate + Transform + LinearLookup + Aggregate.
    /// The lookup contributes ~6% of total pipeline time.
    /// </summary>
    [BenchmarkCategory("Macro")]
    [Benchmark(Baseline = true)]
    public long PipelineLinear()
    {
        long v = ValidateArray(_workload);
        long t = PolynomialTransform(_workload);
        int l = LookupAllLinear(_data, _searchKeys);
        long a = Aggregate(_workload);
        return v ^ t ^ l ^ a;
    }

    /// <summary>
    /// Full pipeline with dictionary lookup. Same Validate + Transform + Aggregate.
    /// Only the lookup step changes. Ratio ~0.92 — 8% improvement.
    /// Amdahl's law predicts 5.9%. The extra ~2% comes from cache effects:
    /// eliminating the linear scan leaves more L1/L2 capacity for subsequent steps.
    /// </summary>
    [BenchmarkCategory("Macro")]
    [Benchmark]
    public long PipelineDictionary()
    {
        long v = ValidateArray(_workload);
        long t = PolynomialTransform(_workload);
        int l = LookupAllDictionary(_searchKeys);
        long a = Aggregate(_workload);
        return v ^ t ^ l ^ a;
    }

    /// <summary>
    /// Validate: sequential scan and checksum. O(n) over the workload array.
    /// ~40% of pipeline time. Does not change between Linear and Dictionary variants.
    /// </summary>
    private static long ValidateArray(int[] data)
    {
        long checksum = 0;
        for (int i = 0; i < data.Length; i++)
            checksum += data[i];
        return checksum;
    }

    /// <summary>
    /// Transform: polynomial computation per element. O(n) over the workload array.
    /// ~40% of pipeline time. multiply/add/xor — enough ALU work to dominate.
    /// </summary>
    private static long PolynomialTransform(int[] data)
    {
        long result = 0;
        for (int i = 0; i < data.Length; i++)
        {
            int v = data[i];
            result += v * 7L + (v ^ (v >> 16));
        }
        return result;
    }

    /// <summary>
    /// Lookup via linear search. 200 keys × Array.IndexOf over 10K elements.
    /// ~6% of pipeline time. This is the step that micro-benchmarks at 262× slower.
    /// </summary>
    private static int LookupAllLinear(int[] data, int[] searchKeys)
    {
        int found = 0;
        for (int i = 0; i < searchKeys.Length; i++)
        {
            if (Array.IndexOf(data, searchKeys[i]) >= 0)
                found++;
        }
        return found;
    }

    /// <summary>
    /// Lookup via dictionary. 200 keys × ContainsKey. O(1) per key.
    /// Replaces the ~6% linear scan with near-zero cost.
    /// </summary>
    private int LookupAllDictionary(int[] searchKeys)
    {
        int found = 0;
        var dict = _dict;
        for (int i = 0; i < searchKeys.Length; i++)
        {
            if (dict.ContainsKey(searchKeys[i]))
                found++;
        }
        return found;
    }

    /// <summary>
    /// Aggregate: running weighted sum. O(n) over the workload array, stride 4.
    /// ~15% of pipeline time. Lighter than Validate/Transform due to stride.
    /// </summary>
    private static long Aggregate(int[] data)
    {
        long aggregate = 0;
        for (int i = 0; i < data.Length; i += 4)
            aggregate += data[i] * 13L;
        return aggregate;
    }
}
