using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 4 — Cache State.
/// Same Get() operation on two table sizes:
/// - 10K entries (~500 KB): dictionary internals fit in L2/L3 cache
/// - 2M entries (~100+ MB): dictionary internals overflow L3, random lookups hit DRAM
///
/// Same algorithm, same key distribution — per-lookup latency differs 3-5x
/// purely because of where data lives in the memory hierarchy.
///
/// On Xeon E5-2697 v2 (30MB L3 per socket):
/// - 10K: bucket array ~80KB + nodes ~400KB = ~500KB → L3 hit, mostly L2
/// - 2M: bucket array ~32MB + nodes ~80MB = ~112MB → DRAM on every random access
/// </summary>
public class E4_CacheState
{
    private const int LookupCount = 100_000;

    /// <summary>
    /// Working set sizes:
    /// - 10_000: fits comfortably in L3 (~500 KB total)
    /// - 2_000_000: exceeds L3 (~112 MB total), forcing DRAM access
    /// </summary>
    [Params(10_000, 2_000_000)]
    public int TableSize { get; set; }

    private ITable<int, Row> _table = null!;
    private int[] _lookupKeys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _table = new StripedTable<int, Row>();

        // Fill table with sequential keys, variable-size rows
        for (int i = 0; i < TableSize; i++)
            _table.Insert(i, Row.Generate(i));

        // Random lookup keys — all valid, no misses
        var rng = new Random(42);
        _lookupKeys = new int[LookupCount];
        for (int i = 0; i < LookupCount; i++)
            _lookupKeys[i] = rng.Next(0, TableSize);
    }

    [GlobalCleanup]
    public void Cleanup() => _table?.Dispose();

    /// <summary>
    /// Random lookups into the table. Same Get() call, same code path.
    /// At 10K entries: L2/L3 hits → fast.
    /// At 2M entries: DRAM access → slow.
    /// OperationsPerInvoke reports per-lookup latency for direct comparison.
    /// </summary>
    [Benchmark(OperationsPerInvoke = LookupCount)]
    public Row? LookupRandom()
    {
        Row? last = default;
        var table = _table;
        var keys = _lookupKeys;
        for (int i = 0; i < LookupCount; i++)
            last = table.Get(keys[i]);
        return last;
    }
}
