using BenchmarkDotNet.Running;
using HardwareCounters;

// Standalone perf profiling mode:
//   dotnet run -c Release -- perf-sequential 8000000
//   dotnet run -c Release -- perf-random 8000000
if (args.Length >= 1 && args[0].StartsWith("perf-"))
{
    int n = args.Length >= 2 ? int.Parse(args[1]) : 8_000_000;
    PerfRunner.Run(args[0], n);
    return;
}

// BDN mode:
//   dotnet run -c Release -- --filter '*'              # all benchmarks (3 dataset sizes)
//   dotnet run -c Release -- --filter *Sequential*     # sequential access only
//   dotnet run -c Release -- --filter *Random*         # random access only
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
