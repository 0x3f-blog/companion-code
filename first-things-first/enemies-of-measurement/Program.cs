// dotnet run -c Release -- --filter *E5*
using BenchmarkDotNet.Running;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
