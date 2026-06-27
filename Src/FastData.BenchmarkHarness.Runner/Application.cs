using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Environment;
using Genbox.FastData.BenchmarkHarness.Runner.Plotting;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal sealed class Application(BenchmarkCatalog catalog)
{
    public async Task<int> RunAsync(Settings settings, CancellationToken cancellationToken)
    {
        ITestData[] structureBenchmarks = TestVectorHelper.GetBenchmarkData(
            settings.WarmupCount,
            settings.MinSampleCount,
            settings.MaxSampleCount,
            settings.TargetIterationTimeMs,
            settings.BenchmarkSize,
            settings.KeyLengthBenchmarkSize,
            settings.Workload,
            settings.MaxError).ToArray();

        ITestData[] earlyExitBenchmarks = TestVectorHelper.GetEarlyExitBenchmarkData(
            settings.WarmupCount,
            settings.MinSampleCount,
            settings.MaxSampleCount,
            settings.TargetIterationTimeMs,
            settings.BenchmarkSize,
            settings.Workload,
            settings.MaxError).ToArray();

        ITestData[] benchmarkData = [..structureBenchmarks, ..earlyExitBenchmarks];
        ResultStore resultStore = new ResultStore(settings.ResultsDirectory);

        return settings.Mode switch
        {
            RunMode.DryRun => DryRun(benchmarkData, settings),
            RunMode.Plot => Plot(benchmarkData, settings, resultStore, false),
            RunMode.IndividualPlot => Plot(benchmarkData, settings, resultStore, true),
            RunMode.Run => await RunBenchmarksAsync(benchmarkData, settings, resultStore, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported benchmark mode '{settings.Mode}'.")
        };
    }

    private int DryRun(ITestData[] benchmarkData, Settings settings)
    {
        string[] names = catalog.GetMatchingNames(benchmarkData, settings);

        if (names.Length == 0)
            return WriteNoBenchmarksMatched(settings);

        foreach (string name in names)
            Console.WriteLine(name);

        return 0;
    }

    private int Plot(ITestData[] benchmarkData, Settings settings, ResultStore resultStore, bool individual)
    {
        History[] histories = catalog.GetHistories(benchmarkData, settings, resultStore).ToArray();

        if (histories.Length == 0)
        {
            ConsoleOutput.WriteError($"No benchmark results matched filter(s): {FormatFilters(settings)}");
            return 1;
        }

        Plotter plotter = new Plotter(settings.Plot);

        if (individual)
            plotter.PlotIndividual(histories);
        else
            plotter.PlotCombined(histories);

        return 0;
    }

    private async Task<int> RunBenchmarksAsync(ITestData[] benchmarkData, Settings settings, ResultStore resultStore, CancellationToken cancellationToken)
    {
        Selection[] selections = catalog.Select(benchmarkData, settings);

        if (selections.Length == 0)
            return WriteNoBenchmarksMatched(settings);

        string? dockerAvailabilityError = await DockerManager.GetAvailabilityErrorAsync(cancellationToken).ConfigureAwait(false);
        if (dockerAvailabilityError != null)
        {
            ConsoleOutput.WriteWarning(dockerAvailabilityError + " Start Docker and rerun the benchmark.");
            return 1;
        }

        CpuAssignment cpu = CpuAssignmentResolver.Resolve(settings);
        using RunEnvironment runEnvironment = RunEnvironment.Apply(settings.Environment, GetExtraRows(settings), cpu.Row);
        ThermalStabilization.Run(cpu.CpuSet, settings.Debug ? ConsoleOutput.WriteDebug : null);

        await using DockerManager dockerManager = new DockerManager(cpuSet: cpu.CpuSet);

        foreach (Selection selection in selections)
        {
            BenchmarkBase harness = selection.Factory(dockerManager);

            if (settings.Debug)
                harness.DebugOutput = ConsoleOutput.WriteDebug;

            foreach (ITestData data in selection.Data)
            {
                string benchmarkName = BenchmarkCatalog.GetBenchmarkName(harness.Name, data);

                try
                {
                    ResultEntry? previousResult = await resultStore.ReadPreviousResultAsync(benchmarkName, cancellationToken).ConfigureAwait(false);
                    BenchmarkResult result = await harness.RunAsync(data, cancellationToken).ConfigureAwait(false);
                    ResultLine resultLine = new ResultLine(
                        harness.Name,
                        data.Identifier,
                        result.Min,
                        result.Max,
                        result.Median,
                        previousResult?.Median,
                        result.Avg,
                        previousResult?.Avg,
                        result.Error,
                        result.StdDev,
                        result.Samples.Length,
                        result.OutlierCount);

                    ConsoleOutput.WriteBenchmarkResult(resultLine, settings.WarningThresholdPercent);

                    await resultStore.AppendResultAsync(benchmarkName, result, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ConsoleOutput.WriteError($"Benchmark '{benchmarkName}' failed: {ex.Message}");
                }
            }
        }

        return 0;
    }

    private static (string Label, string Value)[] GetExtraRows(Settings settings)
    {
        (string Label, string Value)[] rows =
        [
            ("Workload", $"Type: {settings.Workload}, Count: {settings.BenchmarkSize.ToString("N0", NumberFormatInfo.InvariantInfo)}, KeyLength: {settings.KeyLengthBenchmarkSize.ToString("N0", NumberFormatInfo.InvariantInfo)}"),
            ("Loop", $"Warmup: {settings.WarmupCount.ToString(NumberFormatInfo.InvariantInfo)}, Samples: {settings.MinSampleCount.ToString(NumberFormatInfo.InvariantInfo)}-{settings.MaxSampleCount.ToString(NumberFormatInfo.InvariantInfo)}, Target: {settings.TargetIterationTimeMs.ToString(NumberFormatInfo.InvariantInfo)}ms, MaxError: {settings.MaxError.ToString(NumberFormatInfo.InvariantInfo)}%")
        ];

        return settings.Debug ? [..rows, ("Debug", "Enabled")] : rows;
    }

    private static int WriteNoBenchmarksMatched(Settings settings)
    {
        ConsoleOutput.WriteError($"No benchmarks matched filter(s): {FormatFilters(settings)}");
        return 1;
    }

    private static string FormatFilters(Settings settings) => string.Join(", ", settings.Filters);
}