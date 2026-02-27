using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

/// <summary>
/// Reference configuration: how to defend against all 6 enemies in production benchmarks.
///
/// NOTE: The E1–E6 enemy benchmarks intentionally do NOT use this config.
/// They run with minimal/default settings so the enemy effects are visible.
/// This config shows what defenses look like once you know the enemies.
///
/// Usage: [Config(typeof(EnemyDefenseConfig))] on your benchmark class.
///
/// E1 — JIT Warmup:     WarmupCount(3) avoids cold Tier-0 measurements in most cases.
/// E2 — GC Pauses:      Server GC + GcForce between iterations isolates GC impact.
/// E3 — OS Noise:       MinIterationCount(15) improves statistical stability / CI quality.
///                      Affinity/priority are environment controls (often OS-specific), not BDN config.
/// E4 — Cache State:    Not configurable here — match working set size to production reality.
/// E5 — Branch Predict: Not configurable here — use realistic (shuffled) data.
/// E6 — Dead Code:      DisassemblyDiagnoser reveals JIT-eliminated code.
/// </summary>
public class EnemyDefenseConfig : ManualConfig
{
    public EnemyDefenseConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)               // E1: avoid cold Tier-0 measurements in most cases
            .WithGcServer(true)               // E2: Server GC — fewer, larger collections
            .WithGcForce(true)                // E2: force GC between iterations
            .WithMinIterationCount(15)        // E3: improve statistical stability / CI quality
            .WithMaxIterationCount(100)        // E3: let BDN adapt when noise is present
            .WithAffinity((IntPtr)0b11));      // E3: pin to cores 0-1 (adjust per topology)

        AddDiagnoser(MemoryDiagnoser.Default);       // E2: GC pressure visible
        AddDiagnoser(new DisassemblyDiagnoser(          // E6: dead code check
            new DisassemblyDiagnoserConfig(maxDepth: 3)));

        AddColumn(StatisticColumn.StdDev);            // E3: noise visible
    }
}
