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
        await runner.RunAsync(selections, cpu.CpuSet, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static CpuAssignment ResolveCpu(BenchmarkSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Cpu.CpuSet))
            return new CpuAssignment(settings.Cpu.CpuSet, [("Pinned on core", settings.Cpu.CpuSet + " (configured)")]);

        if (!settings.Cpu.AutoSelect)
            return DefaultCpuAssignment();

        CpuSelection? cpuSelection = CpuSelector.TryGetSelection();
        if (cpuSelection is null)
            return new CpuAssignment("0", [("Pinned on core", "0 (auto-select unavailable)")]);

        return new CpuAssignment(cpuSelection.CpuSet, [("Pinned on core", cpuSelection.PhysicalCoreIndex.ToString(CultureInfo.InvariantCulture))]);
    }

    private static CpuAssignment DefaultCpuAssignment() => new CpuAssignment("0", [("Pinned on core", "0 (default)")]);

    private readonly record struct CpuAssignment(string CpuSet, (string Label, string Value)[] Rows);
}