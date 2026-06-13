namespace Genbox.FastData.InternalShared.Harness;

public sealed record BenchmarkResult(double Min, double Median, double Max, double Avg, double Error, double StdDev, long FoundCount, double[] Samples, int OutlierCount);