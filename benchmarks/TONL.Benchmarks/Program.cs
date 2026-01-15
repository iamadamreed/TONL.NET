using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using TONL.NET.Benchmarks;
using TONL.NET.Benchmarks.Reports;

// Check for special commands
if (args.Length > 0 && args[0] == "--size-report")
{
    CrossLanguageReport.GenerateSizeReport();
    return;
}

// Configure BenchmarkDotNet with memory diagnostics
var config = DefaultConfig.Instance
    .AddDiagnoser(MemoryDiagnoser.Default)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

// Run all benchmarks or filter by args
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
