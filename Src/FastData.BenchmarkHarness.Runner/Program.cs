using System.CommandLine;
using System.Globalization;
using System.Text;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Environment;
using Genbox.FastData.BenchmarkHarness.Runner.Execution;
using Genbox.FastData.BenchmarkHarness.Runner.Plotting;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        BenchmarkCatalog catalog = new BenchmarkCatalog();
        RootCommand rootCommand = new BenchmarkCommandLine(catalog).CreateRootCommand((settings, token) => RunAsync(settings, catalog, token));

        try
        {
            ParseResult parseResult = rootCommand.Parse(args, new ParserConfiguration());
            InvocationConfiguration invocationConfig = new InvocationConfiguration { EnableDefaultExceptionHandler = false };
            return await parseResult.InvokeAsync(invocationConfig, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            BenchmarkConsole.WriteError("An error happened: " + ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(BenchmarkSettings settings, BenchmarkCatalog catalog, CancellationToken cancellationToken)
    {
        ITestData[] benchmarkData = catalog.CreateBenchmarkData(settings);
        BenchmarkResultStore resultStore = new BenchmarkResultStore(settings.ResultsDirectory);

        return settings.Mode switch
        {
            BenchmarkMode.DryRun => DryRun(catalog, benchmarkData, settings),
            BenchmarkMode.Plot => Plot(catalog, benchmarkData, settings, resultStore, false),
            BenchmarkMode.IndividualPlot => Plot(catalog, benchmarkData, settings, resultStore, true),
            BenchmarkMode.Run => await RunBenchmarksAsync(catalog, benchmarkData, settings, resultStore, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported benchmark mode '{settings.Mode}'.")
        };
    }

    private static int DryRun(BenchmarkCatalog catalog, ITestData[] benchmarkData, BenchmarkSettings settings)
    {
        string[] names = catalog.GetSelectedNames(benchmarkData, settings).ToArray();

        if (names.Length == 0)
        {
            BenchmarkConsole.WriteError($"No benchmarks matched filter(s): {string.Join(", ", settings.Filters)}");
            return 1;
        }

        foreach (string name in names)
            Console.WriteLine(name);

        return 0;
    }

    private static int Plot(BenchmarkCatalog catalog, ITestData[] benchmarkData, BenchmarkSettings settings, BenchmarkResultStore resultStore, bool individual)
    {
        BenchmarkHistory[] histories = catalog.GetHistories(benchmarkData, settings, resultStore).ToArray();

        if (histories.Length == 0)
        {
            BenchmarkConsole.WriteError($"No benchmark results matched filter(s): {string.Join(", ", settings.Filters)}");
            return 1;
        }

        BenchmarkPlotter plotter = new BenchmarkPlotter(settings.Plot);

        if (individual)
            plotter.PlotIndividual(histories);
        else
            plotter.PlotCombined(histories);

        return 0;
    }

    private static async Task<int> RunBenchmarksAsync(BenchmarkCatalog catalog, ITestData[] benchmarkData, BenchmarkSettings settings, BenchmarkResultStore resultStore, CancellationToken cancellationToken)
    {
        BenchmarkSelection[] selections = catalog.Select(benchmarkData, settings);

        if (selections.Length == 0)
        {
            BenchmarkConsole.WriteError($"No benchmarks matched filter(s): {string.Join(", ", settings.Filters)}");
            return 1;
        }

        CpuAssignment cpu = ResolveCpu(settings);
        using BenchmarkEnvironment benchmarkEnvironment = BenchmarkEnvironment.Apply(settings.Environment, cpu.Rows);
        BenchmarkRunner runner = new BenchmarkRunner(resultStore, settings.DeltaWarningThresholdPercent);
        await runner.RunAsync(selections, cpu.CpuSets, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static CpuAssignment ResolveCpu(BenchmarkSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Cpu.CpuSet))
        {
            if (settings.Parallelism == 1)
                return new CpuAssignment(settings.Cpu.CpuSet, [("Pinned on core", settings.Cpu.CpuSet + " (configured)"), ("Parallelism", "1")]);

            string[] configuredCpuSets = ParseCpuSet(settings.Cpu.CpuSet);
            if (settings.Parallelism > configuredCpuSets.Length)
                throw new InvalidOperationException($"Parallelism {settings.Parallelism} is greater than the {configuredCpuSets.Length} configured CPU core(s) in --cpu-set.");

            WarnIfParallelismMayAddNoise(settings.Parallelism, configuredCpuSets.Length);
            string[] assignedCpuSets = configuredCpuSets.Take(settings.Parallelism).ToArray();
            return new CpuAssignment(assignedCpuSets, GetCpuRows(assignedCpuSets, "configured"));
        }

        if (!settings.Cpu.AutoSelect)
        {
            if (settings.Parallelism > 1)
                throw new InvalidOperationException($"Parallelism {settings.Parallelism} requires at least {settings.Parallelism} available CPU cores, but --no-auto-cpu provides only CPU 0. Use --cpu-set with multiple CPUs or enable auto-select.");

            return DefaultCpuAssignment();
        }

        if (!CpuSelector.TryGetSelections(settings.Parallelism, out CpuSelection[] cpuSelections, out int availableCoreCount))
        {
            if (settings.Parallelism > 1)
                throw new InvalidOperationException($"Parallelism {settings.Parallelism} requires at least {settings.Parallelism} available CPU cores, but automatic CPU selection is unavailable.");

            return new CpuAssignment("0", [("Pinned on core", "0 (auto-select unavailable)")]);
        }

        if (settings.Parallelism > availableCoreCount)
            throw new InvalidOperationException($"Parallelism {settings.Parallelism} is greater than the {availableCoreCount} available CPU core(s).");

        WarnIfParallelismMayAddNoise(settings.Parallelism, availableCoreCount);
        string[] cpuSets = cpuSelections.Select(x => x.CpuSet).ToArray();
        string physicalCores = string.Join(", ", cpuSelections.Select(x => x.PhysicalCoreIndex.ToString(CultureInfo.InvariantCulture)));
        return new CpuAssignment(cpuSets, [("Pinned cores", string.Join(", ", cpuSets) + " (auto-select)"), ("Physical cores", physicalCores), ("Parallelism", settings.Parallelism.ToString(CultureInfo.InvariantCulture))]);
    }

    private static string[] ParseCpuSet(string value)
    {
        List<string> cpuSets = [];
        HashSet<int> seen = [];

        foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int rangeSeparator = part.IndexOf('-', StringComparison.Ordinal);
            if (rangeSeparator < 0)
            {
                AddCpu(ParseCpuIndex(part, value));
                continue;
            }

            int start = ParseCpuIndex(part[..rangeSeparator], value);
            int end = ParseCpuIndex(part[(rangeSeparator + 1)..], value);
            if (end < start)
                throw new InvalidOperationException($"CPU set '{value}' contains an invalid range '{part}'.");

            for (int cpu = start; cpu <= end; cpu++)
                AddCpu(cpu);
        }

        if (cpuSets.Count == 0)
            throw new InvalidOperationException("CPU set must include at least one CPU core.");

        return cpuSets.ToArray();

        void AddCpu(int cpu)
        {
            if (seen.Add(cpu))
                cpuSets.Add(cpu.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static int ParseCpuIndex(string value, string cpuSet)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int cpu) || cpu < 0)
            throw new InvalidOperationException($"CPU set '{cpuSet}' contains invalid CPU index '{value}'.");

        return cpu;
    }

    private static (string Label, string Value)[] GetCpuRows(string[] cpuSets, string source) =>
    [
        (cpuSets.Length == 1 ? "Pinned on core" : "Pinned cores", string.Join(", ", cpuSets) + " (" + source + ")"),
        ("Parallelism", cpuSets.Length.ToString(CultureInfo.InvariantCulture))
    ];

    private static void WarnIfParallelismMayAddNoise(int parallelism, int availableCoreCount)
    {
        if (parallelism <= 1)
            return;

        if (parallelism > Math.Max(1, availableCoreCount / 2))
        {
            BenchmarkConsole.WriteWarning($"Parallelism {parallelism} uses more than half of the {availableCoreCount} available CPU core(s). Shared cache, memory bandwidth, turbo behavior, and Docker overhead can add benchmark noise.");
            return;
        }

        BenchmarkConsole.WriteWarning("Parallel benchmark runs share cache, memory bandwidth, turbo behavior, and Docker overhead. Use --parallelism 1 for lowest-noise measurements.");
    }

    private static CpuAssignment DefaultCpuAssignment() => new CpuAssignment("0", [("Pinned on core", "0 (default)")]);

    private readonly record struct CpuAssignment(string[] CpuSets, (string Label, string Value)[] Rows)
    {
        public CpuAssignment(string cpuSet, (string Label, string Value)[] rows) : this([cpuSet], rows) { }
    }
}