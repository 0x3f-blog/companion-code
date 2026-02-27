using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 1 — JIT Warmup.
/// Summing all payload bytes from a batch read — integrity check after storage engine query.
/// [MethodImpl(NoOptimization)] prevents register promotion, bounds check elimination,
/// loop unrolling, and SIMD vectorization. This simulates what Tier-0 code looks like
/// before the JIT recompiles to Tier-1.
///
/// Same data, same loop, same result — but unoptimized code is 3-10x slower because
/// every iteration pays the full cost: stack-based locals, bounds checks, scalar arithmetic.
/// Check DisassemblyDiagnoser output: Cold has simple scalar loop; Warm has vectorized code
/// with eliminated bounds checks and register-promoted locals.
/// </summary>
[DisassemblyDiagnoser(maxDepth: 3)]
public class E1_JitWarmup
{
    private byte[] _payload = null!;

    /// <summary>
    /// Fill storage engine with N rows, then extract all payloads into a flat byte array.
    /// This simulates a batch read whose results need integrity verification.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        using var table = new StripedTable<int, Row>();
        for (int i = 0; i < EnemySetup.N; i++)
            table.Insert(i, Row.Generate(i));

        // Extract all payloads into contiguous array
        int totalBytes = 0;
        for (int i = 0; i < EnemySetup.N; i++)
        {
            var row = table.Get(i);
            if (row.Data is not null)
                totalBytes += row.Data.Length;
        }

        _payload = new byte[totalBytes];
        int offset = 0;
        for (int i = 0; i < EnemySetup.N; i++)
        {
            var row = table.Get(i);
            if (row.Data is not null)
            {
                row.Data.CopyTo(_payload, offset);
                offset += row.Data.Length;
            }
        }
    }

    /// <summary>
    /// Unoptimized: NoOptimization forces scalar loop — stack-based locals, bounds checks
    /// on every access, no loop unrolling, no SIMD. This is Tier-0.
    /// </summary>
    [Benchmark]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public long SumPayloadCold()
    {
        long sum = 0;
        var data = _payload;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    /// <summary>
    /// Optimized: JIT promotes locals to registers, eliminates bounds checks, may vectorize.
    /// Same data, same loop — but fundamentally different machine code.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long SumPayloadWarm()
    {
        long sum = 0;
        var data = _payload;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }
}
