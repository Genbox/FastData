namespace Genbox.FastData.InternalShared.Harness;

public sealed record BenchmarkResult(double Min, double Median, double Max, double Avg, long FoundCount, double[] Samples, int FilteredSampleCount, int OutlierCount);