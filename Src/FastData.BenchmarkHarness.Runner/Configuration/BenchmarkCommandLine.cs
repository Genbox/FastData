using System.CommandLine;
using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Microsoft.Extensions.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner.Configuration;

internal sealed class BenchmarkCommandLine(BenchmarkCatalog catalog)
{
    private const string ConfigSection = "benchmark";

    private readonly Option<int?> _benchmarkSizeOption = new Option<int?>("--benchmark-size") { Description = "General benchmark key count." };
    private readonly Option<FileInfo?> _configOption = new Option<FileInfo?>("--config") { Description = "The JSON configuration file to read." };
    private readonly Option<string?> _cpuSetOption = new Option<string?>("--cpu-set") { Description = "Explicit Docker CPU set. Overrides automatic CPU selection." };
    private readonly Option<double?> _deltaThresholdOption = new Option<double?>("--delta-threshold") { Description = "Percent delta threshold for red warning output. Default is 5." };
    private readonly Option<bool> _dryRunOption = new Option<bool>("--dry-run") { Description = "List matching benchmarks without running them." };
    private readonly Option<string[]> _filterOption = new Option<string[]>("--filter", "-f") { Description = "BenchmarkDotNet-style wildcard filter. Can be specified multiple times.", AllowMultipleArgumentsPerToken = true };
    private readonly Option<bool> _individualPlotOption = new Option<bool>("--individual-plot") { Description = "Plot one chart per matching benchmark history." };
    private readonly Option<int?> _keyLengthBenchmarkSizeOption = new Option<int?>("--key-length-benchmark-size") { Description = "KeyLength benchmark key count." };
    private readonly Option<string[]> _languageOption = new Option<string[]>("--language") { Description = "Language to include: CSharp, CPlusPlus, or Rust. Can be specified multiple times.", AllowMultipleArgumentsPerToken = true };
    private readonly Option<bool> _noAutoCpuOption = new Option<bool>("--no-auto-cpu") { Description = "Disable automatic CPU selection and use CPU 0 unless --cpu-set is provided." };
    private readonly Argument<string[]> _patternsArgument = new Argument<string[]>("patterns") { Description = "Optional language shorthands or benchmark filters.", Arity = ArgumentArity.ZeroOrMore };
    private readonly Option<int?> _plotHeightOption = new Option<int?>("--plot-height") { Description = "Plot height." };
    private readonly Option<bool> _plotOption = new Option<bool>("--plot") { Description = "Plot matching benchmark histories." };
    private readonly Option<int?> _plotWidthOption = new Option<int?>("--plot-width") { Description = "Plot width. Zero uses console width." };
    private readonly Option<Guid?> _powerPlanOption = new Option<Guid?>("--power-plan") { Description = "Power plan GUID to activate for benchmark runs." };
    private readonly Option<int?> _queryCountOption = new Option<int?>("--query-count") { Description = "Queries per work iteration." };
    private readonly Option<DirectoryInfo?> _resultsDirectoryOption = new Option<DirectoryInfo?>("--results-dir") { Description = "Directory containing benchmark JSONL histories." };
    private readonly Option<int?> _samplesOption = new Option<int?>("--samples") { Description = "Sample count." };
    private readonly Option<int?> _warmupOption = new Option<int?>("--warmup") { Description = "Warmup sample count." };
    private readonly Option<int?> _workIterationsOption = new Option<int?>("--work-iterations") { Description = "Work iterations per sample." };

    public RootCommand CreateRootCommand(Func<BenchmarkSettings, CancellationToken, Task<int>> action)
    {
        RootCommand root = new RootCommand("FastData benchmark harness runner")
        {
            _configOption,
            _dryRunOption,
            _deltaThresholdOption,
            _plotOption,
            _individualPlotOption,
            _filterOption,
            _languageOption,
            _warmupOption,
            _samplesOption,
            _workIterationsOption,
            _queryCountOption,
            _benchmarkSizeOption,
            _keyLengthBenchmarkSizeOption,
            _resultsDirectoryOption,
            _cpuSetOption,
            _noAutoCpuOption,
            _powerPlanOption,
            _plotWidthOption,
            _plotHeightOption,
            _patternsArgument
        };

        root.SetAction(async (parseResult, cancellationToken) =>
        {
            BenchmarkSettings settings = LoadSettings(parseResult);
            return await action(settings, cancellationToken).ConfigureAwait(false);
        });

        return root;
    }

    private BenchmarkSettings LoadSettings(ParseResult parseResult)
    {
        BenchmarkSettings settings = LoadConfiguration(parseResult.GetValue(_configOption));
        ApplyCommandLine(parseResult, settings);
        settings.NormalizeAndValidate();
        ValidateLanguages(settings);
        return settings;
    }

    private static BenchmarkSettings LoadConfiguration(FileInfo? configFile)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddInMemoryCollection(GetDefaults());

        if (configFile != null)
            builder.AddJsonFile(configFile.FullName, false, false);

        builder.AddEnvironmentVariables("FASTDATABENCHMARK_");

        IConfigurationRoot configuration = builder.Build();
        BenchmarkSettings settings = new BenchmarkSettings();
        configuration.GetSection(ConfigSection).Bind(settings);
        return settings;
    }

    private void ApplyCommandLine(ParseResult parseResult, BenchmarkSettings settings)
    {
        List<BenchmarkMode> modeFlags = [];
        if (parseResult.GetValue(_dryRunOption)) modeFlags.Add(BenchmarkMode.DryRun);
        if (parseResult.GetValue(_plotOption)) modeFlags.Add(BenchmarkMode.Plot);
        if (parseResult.GetValue(_individualPlotOption)) modeFlags.Add(BenchmarkMode.IndividualPlot);

        if (modeFlags.Count > 1)
            throw new InvalidOperationException("Only one mode flag can be specified.");

        if (modeFlags.Count == 1)
            settings.Mode = modeFlags[0];

        List<string> filters = [];
        if (HasOption(parseResult, _filterOption))
            filters.AddRange(parseResult.GetValue(_filterOption) ?? []);

        List<string> languages = [];
        if (HasOption(parseResult, _languageOption))
            languages.AddRange(parseResult.GetValue(_languageOption) ?? []);

        string[] patterns = parseResult.GetValue(_patternsArgument) ?? [];
        foreach (string pattern in patterns)
        {
            if (catalog.IsLanguageName(pattern))
                languages.Add(pattern);
            else
                filters.Add(pattern);
        }

        if (filters.Count > 0)
            settings.Filters = filters.ToArray();

        if (languages.Count > 0)
            settings.Languages = languages.ToArray();

        ApplyInt(parseResult, _warmupOption, x => settings.WarmupCount = x);
        ApplyInt(parseResult, _samplesOption, x => settings.SampleCount = x);
        ApplyInt(parseResult, _workIterationsOption, x => settings.WorkIterations = x);
        ApplyInt(parseResult, _queryCountOption, x => settings.QueryCount = x);
        ApplyInt(parseResult, _benchmarkSizeOption, x => settings.BenchmarkSize = x);
        ApplyInt(parseResult, _keyLengthBenchmarkSizeOption, x => settings.KeyLengthBenchmarkSize = x);
        ApplyDouble(parseResult, _deltaThresholdOption, x => settings.DeltaWarningThresholdPercent = x);
        ApplyInt(parseResult, _plotWidthOption, x => settings.Plot.Width = x);
        ApplyInt(parseResult, _plotHeightOption, x => settings.Plot.Height = x);

        if (HasOption(parseResult, _resultsDirectoryOption) && parseResult.GetValue(_resultsDirectoryOption) is {} resultsDirectory)
            settings.ResultsDirectory = resultsDirectory.FullName;

        if (HasOption(parseResult, _cpuSetOption))
        {
            settings.Cpu.CpuSet = parseResult.GetValue(_cpuSetOption);
            settings.Cpu.AutoSelect = false;
        }

        if (parseResult.GetValue(_noAutoCpuOption))
            settings.Cpu.AutoSelect = false;

        if (HasOption(parseResult, _powerPlanOption) && parseResult.GetValue(_powerPlanOption) is {} powerPlan)
            settings.Environment.PowerPlan = powerPlan;
    }

    private static Dictionary<string, string?> GetDefaults()
    {
        BenchmarkSettings settings = new BenchmarkSettings();

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [Key(nameof(BenchmarkSettings.Mode))] = settings.Mode.ToString(),
            [Key(nameof(BenchmarkSettings.WarmupCount))] = ToString(settings.WarmupCount),
            [Key(nameof(BenchmarkSettings.SampleCount))] = ToString(settings.SampleCount),
            [Key(nameof(BenchmarkSettings.WorkIterations))] = ToString(settings.WorkIterations),
            [Key(nameof(BenchmarkSettings.QueryCount))] = ToString(settings.QueryCount),
            [Key(nameof(BenchmarkSettings.BenchmarkSize))] = ToString(settings.BenchmarkSize),
            [Key(nameof(BenchmarkSettings.KeyLengthBenchmarkSize))] = ToString(settings.KeyLengthBenchmarkSize),
            [Key(nameof(BenchmarkSettings.DeltaWarningThresholdPercent))] = ToString(settings.DeltaWarningThresholdPercent),
            [Key(nameof(BenchmarkSettings.ResultsDirectory))] = settings.ResultsDirectory,
            [Key(nameof(BenchmarkSettings.Cpu), nameof(CpuSettings.AutoSelect))] = ToString(settings.Cpu.AutoSelect),
            [Key(nameof(BenchmarkSettings.Environment), nameof(BenchmarkEnvironmentSettings.PowerPlan))] = settings.Environment.PowerPlan.ToString("D"),
            [Key(nameof(BenchmarkSettings.Plot), nameof(PlotSettings.Width))] = ToString(settings.Plot.Width),
            [Key(nameof(BenchmarkSettings.Plot), nameof(PlotSettings.Height))] = ToString(settings.Plot.Height),
            [Key(nameof(BenchmarkSettings.Plot), nameof(PlotSettings.MaxXTickLabels))] = ToString(settings.Plot.MaxXTickLabels)
        };
    }

    private static string Key(params string[] path) => ConfigurationPath.Combine([ConfigSection, ..path]);

    private static string ToString(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string ToString(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string ToString(bool value) => value.ToString(CultureInfo.InvariantCulture);

    private static void ApplyInt(ParseResult parseResult, Option<int?> option, Action<int> apply)
    {
        if (HasOption(parseResult, option) && parseResult.GetValue(option) is {} value)
            apply(value);
    }

    private static void ApplyDouble(ParseResult parseResult, Option<double?> option, Action<double> apply)
    {
        if (HasOption(parseResult, option) && parseResult.GetValue(option) is {} value)
            apply(value);
    }

    private static bool HasOption<T>(ParseResult parseResult, Option<T> option) => parseResult.GetResult(option) != null;

    private void ValidateLanguages(BenchmarkSettings settings)
    {
        foreach (string language in settings.Languages)
        {
            if (!catalog.IsLanguageName(language))
                throw new InvalidOperationException($"Language '{language}' is invalid. Use CSharp, CPlusPlus, or Rust.");
        }
    }
}