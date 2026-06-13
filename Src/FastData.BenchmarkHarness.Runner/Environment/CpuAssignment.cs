namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal readonly record struct CpuAssignment(string CpuSet, (string Label, string Value) Row);