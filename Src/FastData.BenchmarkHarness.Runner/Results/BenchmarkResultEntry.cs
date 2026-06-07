namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal sealed record BenchmarkResultEntry(string Name, double Min, double Median, double Max, double Avg, DateTimeOffset TimestampUtc);