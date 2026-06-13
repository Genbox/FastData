namespace Genbox.FastData.BenchmarkHarness.Runner;

internal readonly record struct ResultLine(
    string HarnessName,
    string DataIdentifier,
    double Min,
    double Max,
    double Median,
    double? PreviousMedian,
    double Avg,
    double? PreviousAvg,
    double Error,
    double StdDev,
    int Samples,
    int Outliers);