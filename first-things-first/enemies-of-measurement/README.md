# Enemies of Measurement — Companion Code

Companion code for [First Things First: Enemies of Measurement](https://0x3f.blog/posts/first-things-first-enemies-of-measurement/).

Six enemies that change benchmark results 2–6× — even under BenchmarkDotNet.

## Run

```bash
# All benchmarks
dotnet run -c Release

# Single enemy
dotnet run -c Release -- --filter *E5*

# System noise comparison (Enemy 3)
# Run 1 — noisy (normal system):
dotnet run -c Release -- --filter *E3*

# Run 2 — isolated (Linux only):
taskset -c 0 dotnet run -c Release -- --filter *E3*
```

## Measured results (dual Xeon E5-2697 v2)

| Enemy                  | Variant A                         | Variant B                          | Ratio       |
|------------------------|-----------------------------------|------------------------------------|-------------|
| E1 — JIT Optimization  | Unoptimized (49.764 ms / 124 B)   | Optimized (8.247 ms / 49 B)       | 6.0×        |
| E2 — GC Pauses         | AllocHeavy (36.81 ms / 23.9 MB)   | PreAllocated (16.35 ms / 7.52 MB) | 2.3×        |
| E3 — System Noise      | Noisy (StdDev 0.945 ms)           | Isolated (StdDev 0.254 ms)        | 3.7× StdDev |
| E4 — Cache State       | 2M entries (50.08 ns/lookup)       | 10K entries (17.05 ns/lookup)      | 2.9×        |
| E5 — Branch Predictor  | Random (41.363 ms)                 | Sorted (8.214 ms)                  | 5.0×        |
| E6 — Dead Code         | Eliminated (3.750 ms / 21 B)      | Preserved (22.220 ms / 66 B)      | 5.9×        |

## Requirements

- .NET 9.0+
- BenchmarkDotNet 0.14.*
