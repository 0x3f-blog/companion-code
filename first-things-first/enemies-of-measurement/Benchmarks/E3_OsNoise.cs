using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 3 — OS Scheduler Noise.
/// Two identical insert methods on the same table. The difference is NOT in the code —
/// it's in the running conditions.
///
/// The contrast emerges between two separate runs:
///   Run 1 (noisy):    dotnet run -c Release -- --filter *E3*
///   Run 2 (isolated): taskset -c 0 nice -n -20 dotnet run -c Release -- --filter *E3*
///
/// Compare StdDev between runs. Same code, same data, different variance.
/// On noisy runs: context switches inject 10-100us jitter into iterations.
/// On isolated runs: pinned core + high priority = tight, repeatable measurements.
///
/// NOTE: Unlike E2, this benchmark intentionally uses GlobalSetup (not IterationSetup).
/// The table is pre-populated once — every benchmark iteration does updates, not inserts.
/// This gives deterministic, low-variance work where OS noise is the only variable.
/// Fresh-table inserts would add ConcurrentDictionary growth variance that drowns the
/// OS noise signal we're trying to measure.
/// </summary>
public class E3_OsNoise
{
    private ITable<int, Row> _table = null!;
    private int[] _keys = null!;
    private Row[] _rows = null!;

    [GlobalSetup]
    public void Setup()
    {
        _table = new StripedTable<int, Row>();
        var rng = new Random(42);
        _keys = new int[EnemySetup.N];
        _rows = new Row[EnemySetup.N];
        for (int i = 0; i < EnemySetup.N; i++)
        {
            _keys[i] = rng.Next(0, EnemySetup.KeySpace);
            _rows[i] = Row.Generate(_keys[i]);
        }

        // Pre-populate: every subsequent Insert is an update, not a growth event.
        for (int i = 0; i < EnemySetup.N; i++)
            _table.Insert(_keys[i], _rows[i]);
    }

    [GlobalCleanup]
    public void Cleanup() => _table?.Dispose();

    /// <summary>Baseline insert (update). Identical to InsertSame.</summary>
    [Benchmark(Baseline = true)]
    public void InsertBaseline()
    {
        for (int i = 0; i < EnemySetup.N; i++)
            _table.Insert(_keys[i], _rows[i]);
    }

    /// <summary>Same insert (update). Proves the difference is environmental, not algorithmic.</summary>
    [Benchmark]
    public void InsertSame()
    {
        for (int i = 0; i < EnemySetup.N; i++)
            _table.Insert(_keys[i], _rows[i]);
    }
}
