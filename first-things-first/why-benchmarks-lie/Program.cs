// dotnet run -c Release -- --filter *WhyBenchmarksLie*
using BenchmarkDotNet.Running;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
