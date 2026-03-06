# Statistics That Matter — Companion Code

Companion code for [First Things First: Statistics That Matter](https://0x3f.blog/posts/first-things-first-statistics-that-matter/).

Three layers between a number and a conclusion.

## Run

```bash
# Pin to a single NUMA node to eliminate cross-socket variance
# (adjust core range for your topology — 0-11 = one socket on dual E5-2697 v2)
taskset -c 0-11 dotnet run -c Release
taskset -c 0-11 dotnet run -c Release -- --filter '*NoisyComparison*'
taskset -c 0-11 dotnet run -c Release -- --filter '*EffectSizeDemo*'
taskset -c 0-11 dotnet run -c Release -- --filter '*MicroVsMacro*'

# 5-iteration CI demo (result flips)
taskset -c 0-11 dotnet run -c Release -- --filter '*NoisyComparison*' --iterationCount 5 --warmupCount 3
```

## Results (dual Xeon E5-2697 v2)

*Run on your hardware for your own numbers.*

### NoisyComparison (20 iterations)

| Method        | N        | Mean     | Error    | StdDev   | Ratio |
|-------------- |--------- |---------:|---------:|---------:|------:|
| FilterBranch  | 20000000 | 25.25 ms | 0.173 ms | 0.177 ms |  1.00 |
| FilterTernary | 20000000 | 25.64 ms | 0.111 ms | 0.109 ms |  1.02 |

### EffectSizeDemo

| Method       | Categories  | N       | Mean         | Ratio |
|------------- |------------ |-------- |-------------:|------:|
| SumArray     | SmallEffect | 1000000 |     512.7 us |  1.00 |
| SumSpan      | SmallEffect | 1000000 |     515.3 us |  1.01 |
| SearchLinear | LargeEffect | 1000000 | 248,303.3 us | 1.000 |
| SearchBinary | LargeEffect | 1000000 |     231.8 us | 0.001 |

### MicroVsMacro

| Method             | Categories | Mean         | Ratio |
|------------------- |----------- |-------------:|------:|
| LookupLinear       | Micro      |   412.089 us | 1.000 |
| LookupDictionary   | Micro      |     1.571 us | 0.004 |
| PipelineLinear     | Macro      | 7,181.1 us   |  1.00 |
| PipelineDictionary | Macro      | 6,612.0 us   |  0.92 |

## Requirements

- .NET 9.0+
- BenchmarkDotNet 0.14.*
