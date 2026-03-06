// dotnet run -c Release -- --filter '*EffectSizeDemo*'
using BenchmarkDotNet.Running;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
