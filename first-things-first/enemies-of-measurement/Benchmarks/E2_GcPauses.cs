using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 2 — GC Pauses.
/// Row.Generate(key) allocates a new byte[32-256] per insert — 100K inserts = 100K allocations.
/// GC fires mid-measurement: Gen0/Gen1 collections inflate Mean and spike StdDev.
///
/// Pre-allocating rows in GlobalSetup eliminates GC pressure from the hot path.
/// Same inserts, same table, same data — but one triggers collection storms.
/// </summary>
[MemoryDiagnoser]
public class E2_GcPauses
{
    private ITable<int, Row> _table = null!;
    private int[] _keys = null!;
    private Row[] _preAllocated = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _keys = new int[EnemySetup.N];
        _preAllocated = new Row[EnemySetup.N];
        for (int i = 0; i < EnemySetup.N; i++)
        {
            _keys[i] = rng.Next(0, EnemySetup.KeySpace);
            _preAllocated[i] = Row.Generate(_keys[i]);
        }
    }

    /// <summary>Fresh empty table per iteration — every Insert is a true insert, not an update.</summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _table?.Dispose();
        _table = new StripedTable<int, Row>();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _table?.Dispose();
        _table = null!;
    }

    /// <summary>
    /// Bad: allocates Row.Generate(key) per insert — new byte[] every time.
    /// MemoryDiagnoser shows Gen0/Gen1 pressure. StdDev shows timing instability.
    /// </summary>
    [Benchmark]
    public void InsertAllocHeavy()
    {
        for (int i = 0; i < EnemySetup.N; i++)
            _table.Insert(_keys[i], Row.Generate(_keys[i]));
    }

    /// <summary>
    /// Good: uses pre-allocated rows from GlobalSetup — zero allocation in hot path.
    /// Same data, same inserts — stable measurement.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void InsertPreAllocated()
    {
        for (int i = 0; i < EnemySetup.N; i++)
            _table.Insert(_keys[i], _preAllocated[i]);
    }
}
