# Hardware Counters — Companion Code

Sequential vs random memory access: same data, same operation, different access order. BenchmarkDotNet says 12.9× slower — but cannot explain *why* or predict how the ratio changes when the dataset grows. `perf stat` reveals the mechanism (cache misses, IPC collapse) and predicts the cliff: 12.9× → 19.5× when the working set exceeds L3.

## Quick start

```bash
# All benchmarks — 3 dataset sizes (1M, 8M, 64M elements)
dotnet run -c Release -- --filter '*'

# Individual variants
dotnet run -c Release -- --filter *Sequential*
dotnet run -c Release -- --filter *Random*
```

## perf stat (Linux)

```bash
# Compare hardware counters between variants
perf stat -e cycles,instructions,cache-references,cache-misses,branch-misses \
    dotnet run -c Release -- --filter *Sequential*

perf stat -e cycles,instructions,cache-references,cache-misses,branch-misses \
    dotnet run -c Release -- --filter *Random*
```

Key counters to compare:
- **IPC** (instructions / cycles) — sequential should be 2-3×, random ~1.0 or less
- **cache-miss rate** (cache-misses / cache-references) — random access shows much higher miss rate
- **branch-misses** — both should be similar (no branching difference)

## Scripts

```bash
# Automated perf stat comparison
./Scripts/perf-stat.sh

# Flame graph generation (requires FlameGraph tools)
FLAMEGRAPH_DIR=~/FlameGraph ./Scripts/flame-graph.sh

# Full scaling run: BDN + perf stat at all sizes
./Scripts/run-scaling.sh
```

## What to expect

At 1M elements (8 MB — fits in L3): BDN Ratio 12.88×. Random access is obviously slower — but the *cause* is hidden in L1/L2 cache misses absorbed by L3.

At 8M elements (64 MB — exceeds L3): Ratio jumps to 19.53×. `perf stat` predicted it: IPC 0.42 and L1 miss rate 24.90% at 1M were red flags for a memory-bound workload that would collapse when L3 ran out.

## Requirements

- .NET 9.0 SDK
- Linux with `perf` for hardware counters (optional — BDN runs on any OS)
- [FlameGraph](https://github.com/brendangregg/FlameGraph) tools for SVG generation (optional)
