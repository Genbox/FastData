namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal sealed record ResultEntry(string Name, double Min, double Median, double Max, double Avg, double Error, double StdDev, DateTimeOffset TimestampUtc);