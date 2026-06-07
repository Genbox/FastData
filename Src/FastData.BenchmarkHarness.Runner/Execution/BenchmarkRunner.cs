using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Execution;

internal sealed class BenchmarkRunner(BenchmarkResultStore resultStore, double deltaWarningThresholdPercent)
{
    public async Task RunAsync(IEnumerable<BenchmarkSelection> selections, string cpuSet, CancellationToken cancellationToken)
    {
        foreach (BenchmarkSelection selection in selections)
            await RunHarnessAsync(selection.Factory, selection.Data, cpuSet, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask RunHarnessAsync(Func<DockerManager, BenchmarkBase> harnessFactory, IEnumerable<ITestData> benchmarkData, string cpuSet, CancellationToken cancellationToken)
    {
        await using DockerManager dockerManager = new DockerManager(cpuSet: cpuSet);
        BenchmarkBase harness = harnessFactory(dockerManager);

        foreach (ITestData data in benchmarkData)
        {
            string benchmarkName = BenchmarkCatalog.GetBenchmarkName(harness.Name, data);
            BenchmarkResultEntry? previousResult = await resultStore.ReadPreviousResultAsync(benchmarkName, cancellationToken).ConfigureAwait(false);
            BenchmarkResult result = await harness.RunAsync(data, cancellationToken).ConfigureAwait(false);

            BenchmarkConsole.WriteBenchmarkResult(CreateResultLine(harness.Name, data.Identifier, result, previousResult));
            await resultStore.AppendResultAsync(benchmarkName, result, cancellationToken).ConfigureAwait(false);
        }
    }

    private BenchmarkResultLine CreateResultLine(string harnessName, string dataIdentifier, BenchmarkResult result, BenchmarkResultEntry? previousResult) => new BenchmarkResultLine(
        harnessName,
        dataIdentifier,
        FormatResult(result.Min),
        FormatResult(result.Max),
        FormatResult(result.Median),
        FormatDelta(result.Median, previousResult?.Median),
        FormatResult(result.Avg),
        FormatDelta(result.Avg, previousResult?.Avg));

    private BenchmarkResultDelta FormatDelta(double current, double? previous)
    {
        if (previous is null)
            return new BenchmarkResultDelta("n/a", false);

        if (previous.Value == 0)
            return new BenchmarkResultDelta(current == 0 ? "0%" : "n/a", false);

        double delta = ((current - previous.Value) / previous.Value) * 100;
        string text = delta.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%";
        return new BenchmarkResultDelta(text, Math.Abs(delta) >= deltaWarningThresholdPercent);
    }

    private static string FormatResult(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}