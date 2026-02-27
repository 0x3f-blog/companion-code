using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 6 — Dead Code Elimination.
/// Computing a checksum over query results — but forgetting to return it.
/// The JIT proves the accumulation has no side effects and strips it out,
/// but keeps the loop counter — so the method still runs, just ~6× faster
/// (3.8 ms vs 22.4 ms). The real tell is Code Size: 21 B vs 66 B.
///
/// The formula is Row.Generate's payload computation: 32 + (i % 225).
/// Pure arithmetic — no memory access, no exceptions, no side effects.
/// Return the result = real measurement. Don't return = partial phantom.
/// </summary>
[DisassemblyDiagnoser(maxDepth: 3)]
public class E6_DeadCode
{
    [Params(10_000_000)]
    public int N { get; set; }

    /// <summary>
    /// Bad: checksum never returned. The JIT strips the accumulation (checksum +=)
    /// but keeps the loop counter — Code Size drops from 66 B to 21 B.
    /// Check the disassembly: the add is gone, only inc+cmp+jl remain.
    /// </summary>
    [Benchmark]
    public void ChecksumEliminated()
    {
        long checksum = 0;
        for (int i = 0; i < N; i++)
            checksum += 32 + (i % 225);
        // checksum not returned — JIT drops the loop
    }

    /// <summary>
    /// Good: checksum returned. The JIT must compute the value because BenchmarkDotNet
    /// consumes it. This is the actual cost of the computation.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long ChecksumPreserved()
    {
        long checksum = 0;
        for (int i = 0; i < N; i++)
            checksum += 32 + (i % 225);
        return checksum;
    }
}
