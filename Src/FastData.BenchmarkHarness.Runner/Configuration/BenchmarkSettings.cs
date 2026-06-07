namespace Genbox.FastData.BenchmarkHarness.Runner.Configuration;

internal sealed class BenchmarkSettings
{
    public BenchmarkMode Mode { get; set; } = BenchmarkMode.Run;
    public string[] Filters { get; set; } = [];
    public string[] Languages { get; set; } = [];
    public int WarmupCount { get; set; } = 3;
    public int SampleCount { get; set; } = 6;
    public int WorkIterations { get; set; } = 1_000_000;
    public int QueryCount { get; set; } = 25;
    public int BenchmarkSize { get; set; } = 1000;
    public int KeyLengthBenchmarkSize { get; set; } = 128;
    public double DeltaWarningThresholdPercent { get; set; } = 5;
    public string ResultsDirectory { get; set; } = "benchmark-results";
    public CpuSettings Cpu { get; set; } = new CpuSettings();
    public BenchmarkEnvironmentSettings Environment { get; set; } = new BenchmarkEnvironmentSettings();
    public PlotSettings Plot { get; set; } = new PlotSettings();

    public void NormalizeAndValidate()
    {
        Filters = Filters.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        if (Filters.Length == 0)
            Filters = ["*"];

        Languages = Languages.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

        ValidatePositive(WarmupCount, nameof(WarmupCount));
        ValidatePositive(SampleCount, nameof(SampleCount));
        ValidatePositive(WorkIterations, nameof(WorkIterations));
        ValidatePositive(QueryCount, nameof(QueryCount));
        ValidatePositive(BenchmarkSize, nameof(BenchmarkSize));
        ValidatePositive(KeyLengthBenchmarkSize, nameof(KeyLengthBenchmarkSize));
        ValidateNonNegative(DeltaWarningThresholdPercent, nameof(DeltaWarningThresholdPercent));
        ValidatePositive(Plot.Height, nameof(Plot.Height));
        ValidatePositive(Plot.MaxXTickLabels, nameof(Plot.MaxXTickLabels));

        if (Plot.Width < 0)
            throw new InvalidOperationException("Plot.Width must be zero or a positive integer.");

        if (string.IsNullOrWhiteSpace(ResultsDirectory))
            throw new InvalidOperationException("ResultsDirectory must be provided.");
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
            throw new InvalidOperationException(name + " must be a positive integer.");
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (value < 0 || double.IsNaN(value))
            throw new InvalidOperationException(name + " must be zero or a positive number.");
    }
}