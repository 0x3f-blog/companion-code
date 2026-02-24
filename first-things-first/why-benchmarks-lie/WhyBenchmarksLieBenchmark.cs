using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

/// <summary>Lock = Dictionary + single global lock. Striped = ConcurrentDictionary (lock striping).</summary>
public enum Backend { Lock, Striped }

/// <summary>Shared constants and factory for both benchmark classes.</summary>
internal static class WhyBenchmarksLieSetup
{
    public const int N = 500_000;
    public const int KeySpace = 1_000_000;

    private const string WalRoot = "/var/tmp/bench-wal";

    public static string CreateWalRunDirectory()
    {
        var walDir = Path.Combine(WalRoot, $"run-{Guid.NewGuid():N}");
        Directory.CreateDirectory(walDir);
        return walDir;
    }

    public static string CreateWalPath(string walDir, Backend backend, string suffix)
        => Path.Combine(walDir, $"wal-{backend}-{suffix}.log");

    public static ITable<int, Row> CreateTable(Backend backend, string walPath)
        => backend == Backend.Lock
            ? new LockTable<int, Row>(walPath: walPath)
            : new StripedTable<int, Row>(walPath: walPath);

    public static void ValidateWorkSplit(int threadCount)
    {
        if (threadCount <= 0)
            throw new InvalidOperationException($"ThreadCount must be positive, got {threadCount}.");
        if (N % threadCount != 0)
            throw new InvalidOperationException(
                $"N ({N}) must be divisible by ThreadCount ({threadCount}) for OperationsPerInvoke to be accurate.");
    }
}

/// <summary>
/// LAZY — the flat line. Sequential keys, single thread, no WAL flush.
/// ThreadCount is declared but unused — produces the flat-line chart data.
/// </summary>
[ShortRunJob(RuntimeMoniker.Net90)]
public class WhyBenchmarksLieSequentialBenchmark
{
    private ITable<int, Row> _table = null!;
    private string _walDir = string.Empty;

    [Params(Backend.Lock, Backend.Striped)]
    public Backend BackendType { get; set; }

    [Params(1, 4, 16, 32)]
    public int ThreadCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _walDir = WhyBenchmarksLieSetup.CreateWalRunDirectory();
        var walPath = WhyBenchmarksLieSetup.CreateWalPath(_walDir, BackendType, "sequential");
        _table = WhyBenchmarksLieSetup.CreateTable(BackendType, walPath);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _table?.Dispose();
        _table = null!;
        if (Directory.Exists(_walDir))
            Directory.Delete(_walDir, recursive: true);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = WhyBenchmarksLieSetup.N)]
    public void InsertSequential()
    {
        for (int i = 0; i < WhyBenchmarksLieSetup.N; i++)
            _table.Insert(i, Row.Default);
    }
}

/// <summary>
/// NARROW / REALISTIC / NOFLUSH — parallel scenarios.
/// GlobalSetup: table persists across iterations (steady-state overwrites).
/// </summary>
[ShortRunJob(RuntimeMoniker.Net90)]
public class WhyBenchmarksLieBenchmark
{
    private ITable<int, Row> _table = null!;
    private int[] _randomKeys = null!;
    private Row[] _randomRows = null!;
    private string _walDir = string.Empty;

    [Params(Backend.Lock, Backend.Striped)]
    public Backend BackendType { get; set; }

    [Params(1, 4, 16, 32)]
    public int ThreadCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        WhyBenchmarksLieSetup.ValidateWorkSplit(ThreadCount);
        _walDir = WhyBenchmarksLieSetup.CreateWalRunDirectory();
        var walPath = WhyBenchmarksLieSetup.CreateWalPath(_walDir, BackendType, $"parallel-{ThreadCount}");
        _table = WhyBenchmarksLieSetup.CreateTable(BackendType, walPath);

        var rng = new Random(42);
        _randomKeys = new int[WhyBenchmarksLieSetup.N];
        _randomRows = new Row[WhyBenchmarksLieSetup.N];
        for (int i = 0; i < WhyBenchmarksLieSetup.N; i++)
        {
            _randomKeys[i] = rng.Next(0, WhyBenchmarksLieSetup.KeySpace);
            _randomRows[i] = Row.Generate(_randomKeys[i]);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _table?.Dispose();
        _table = null!;
        if (Directory.Exists(_walDir))
            Directory.Delete(_walDir, recursive: true);
    }

    /// <summary>Parallel threads, sequential keys per partition, no fsync.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = WhyBenchmarksLieSetup.N)]
    public void InsertNarrow()
    {
        int opsPerThread = WhyBenchmarksLieSetup.N / ThreadCount;
        var table = _table;
        var options = new ParallelOptions { MaxDegreeOfParallelism = ThreadCount };
        Parallel.For(0, ThreadCount, options, threadIdx =>
        {
            int start = threadIdx * opsPerThread;
            for (int i = 0; i < opsPerThread; i++)
                table.Insert(start + i, Row.Default);
        });
    }

    /// <summary>Random keys, variable rows, WAL + fsync — production conditions.</summary>
    [Benchmark(OperationsPerInvoke = WhyBenchmarksLieSetup.N)]
    public void InsertRealistic()
    {
        int opsPerThread = WhyBenchmarksLieSetup.N / ThreadCount;
        var table = _table;
        var randomKeys = _randomKeys;
        var randomRows = _randomRows;
        var options = new ParallelOptions { MaxDegreeOfParallelism = ThreadCount };
        Parallel.For(0, ThreadCount, options, threadIdx =>
        {
            int start = threadIdx * opsPerThread;
            for (int i = 0; i < opsPerThread; i++)
                table.Insert(randomKeys[start + i], randomRows[start + i]);
        });
        table.FlushWAL();
    }

    /// <summary>Same as Realistic minus FlushWAL — isolates fsync cost.</summary>
    [Benchmark(OperationsPerInvoke = WhyBenchmarksLieSetup.N)]
    public void InsertRealisticNoFlush()
    {
        int opsPerThread = WhyBenchmarksLieSetup.N / ThreadCount;
        var table = _table;
        var randomKeys = _randomKeys;
        var randomRows = _randomRows;
        var options = new ParallelOptions { MaxDegreeOfParallelism = ThreadCount };
        Parallel.For(0, ThreadCount, options, threadIdx =>
        {
            int start = threadIdx * opsPerThread;
            for (int i = 0; i < opsPerThread; i++)
                table.Insert(randomKeys[start + i], randomRows[start + i]);
        });
    }
}
