namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal readonly record struct BenchmarkResultLine(
    string HarnessName,
    string DataIdentifier,
    string Min,
    string Max,
    string Median,
    BenchmarkResultDelta MedianDelta,
    string Avg,
    BenchmarkResultDelta AvgDelta);