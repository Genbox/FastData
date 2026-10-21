namespace Genbox.FastData.Ir.Harness.Runner;

internal sealed record BenchmarkSample(double OverheadNs, double ElapsedNs, long Found);