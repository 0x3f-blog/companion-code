# Why Benchmarks Lie — Companion Code

Companion code for [First Things First: Why Benchmarks Lie](https://0x3f.blog/posts/first-things-first-why-benchmarks-lie/).

Three benchmarks. No production code. Just playing with numbers — same two classes, three verdicts.

## Run

```bash
# All benchmarks
dotnet run -c Release

# Single benchmark
dotnet run -c Release -- --filter *WhyBenchmarksLie*
```

## What it benchmarks

| Benchmark | What it tests                  | Winner                                            |
|-----------|--------------------------------|---------------------------------------------------|
| Lazy      | Single-threaded, no contention | Dictionary + lock (~2–3×)                         |
| Narrow    | Parallel inserts, no I/O       | ConcurrentDictionary (~5–20×, ShortRun variance)  |
| Realistic | Parallel inserts + WAL fsync   | Neither (~120–130K ops/sec)                       |

## Requirements

- .NET 9.0+
- BenchmarkDotNet 0.14.*
