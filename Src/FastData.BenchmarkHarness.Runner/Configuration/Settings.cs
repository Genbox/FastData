using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Configuration;

internal sealed class Settings
{
    public RunMode Mode { get; set; } = RunMode.Run;
    public string[] Filters { get; set; } = [];
    public string[] Languages { get; set; } = [];
    public BenchmarkWorkload Workload { get; set; } = BenchmarkWorkload.Mixed;
    public int WarmupCount { get; set; } = 5;
    public int MinSampleCount { get; set; } = 9; // Keep sample counts odd so the reported median is an observed sample rather than an interpolated value.
    public int MaxSampleCount { get; set; } = 35;
    public double MaxError { get; set; } = 2.0;
    public int TargetIterationTimeMs { get; set; } = 250;
    public int BenchmarkSize { get; set; } = 1000;
    public int KeyLengthBenchmarkSize { get; set; } = 64;
    public double WarningThresholdPercent { get; set; } = 10;
    public bool Debug { get; set; }
    public string ResultsDirectory { get; set; } = ResolveDefaultResultsDirectory();
    public CpuSettings Cpu { get; set; } = new CpuSettings();
    public EnvironmentSettings Environment { get; set; } = new EnvironmentSettings();
    public PlotSettings Plot { get; set; } = new PlotSettings();

    public void NormalizeAndValidate()
    {
        Filters = Filters.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        if (Filters.Length == 0)
            Filters = ["*"];

        Languages = Languages.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

        ValidatePositive(WarmupCount, nameof(WarmupCount));
        ValidatePositive(MinSampleCount, nameof(MinSampleCount));
        ValidatePositive(MaxSampleCount, nameof(MaxSampleCount));
        ValidatePositive(MaxError, nameof(MaxError));
        ValidatePositive(TargetIterationTimeMs, nameof(TargetIterationTimeMs));
        ValidatePositive(BenchmarkSize, nameof(BenchmarkSize));
        ValidatePositive(KeyLengthBenchmarkSize, nameof(KeyLengthBenchmarkSize));
        ValidateNonNegative(WarningThresholdPercent, nameof(WarningThresholdPercent));
        ValidatePositive(Plot.Height, nameof(Plot.Height));
        ValidatePositive(Plot.MaxXTickLabels, nameof(Plot.MaxXTickLabels));

        if (Plot.Width < 0)
            throw new InvalidOperationException("Plot.Width must be zero or a positive integer.");

        if (MinSampleCount % 2 == 0)
            throw new InvalidOperationException(nameof(MinSampleCount) + " must be an odd integer so the median is an observed sample.");

        if (MaxSampleCount % 2 == 0)
            throw new InvalidOperationException(nameof(MaxSampleCount) + " must be an odd integer so the median is an observed sample.");

        if (MaxSampleCount < MinSampleCount)
            throw new InvalidOperationException(nameof(MaxSampleCount) + " must be greater than or equal to " + nameof(MinSampleCount) + ".");

        if (!Enum.IsDefined(Workload))
            throw new InvalidOperationException("Workload must be Hit, Miss, or Mixed.");

        if (string.IsNullOrWhiteSpace(ResultsDirectory))
            throw new InvalidOperationException("ResultsDirectory must be provided.");
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
            throw new InvalidOperationException(name + " must be a positive integer.");
    }

    private static void ValidatePositive(double value, string name)
    {
        if (value <= 0 || !double.IsFinite(value))
            throw new InvalidOperationException(name + " must be a positive, finite number.");
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (value < 0 || !double.IsFinite(value))
            throw new InvalidOperationException(name + " must be zero or a positive, finite number.");
    }

    private static string ResolveDefaultResultsDirectory()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FastData.slnx")))
                return Path.Combine(directory.FullName, "BenchmarkResults");

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "BenchmarkResults");
    }
}