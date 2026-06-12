using System.Collections.Concurrent;
using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Execution;

internal sealed class BenchmarkRunner(BenchmarkResultStore resultStore, double deltaWarningThresholdPercent)
{
    private readonly Lock _consoleLock = new Lock();

    public async Task RunAsync(IEnumerable<BenchmarkSelection> selections, string[] cpuSets, CancellationToken cancellationToken)
    {
        if (cpuSets.Length == 0)
            throw new ArgumentException("At least one CPU set must be provided.", nameof(cpuSets));

        if (cpuSets.Length == 1)
        {
            foreach (BenchmarkSelection selection in selections)
                await RunHarnessAsync(selection.Factory, selection.Data, cpuSets[0], cancellationToken).ConfigureAwait(false);

            return;
        }

        BenchmarkJob[] jobs = selections.SelectMany(x => x.Data.Select(y => new BenchmarkJob(x.Factory, y))).ToArray();
        ConcurrentQueue<BenchmarkJob> queue = new ConcurrentQueue<BenchmarkJob>(jobs);
        Task[] workers = new Task[cpuSets.Length];

        for (int i = 0; i < cpuSets.Length; i++)
            workers[i] = RunWorkerAsync(i, cpuSets[i], queue, cancellationToken);

        await Task.WhenAll(workers).ConfigureAwait(false);
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

    private async Task RunWorkerAsync(int workerIndex, string cpuSet, ConcurrentQueue<BenchmarkJob> queue, CancellationToken cancellationToken)
    {
        string containerPrefix = "fastdata-benchmark-" + workerIndex.ToString(CultureInfo.InvariantCulture) + "-worker";
        await using DockerManager dockerManager = new DockerManager(containerPrefix: containerPrefix, cpuSet: cpuSet);

        while (queue.TryDequeue(out BenchmarkJob job))
            await RunJobAsync(dockerManager, job, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunJobAsync(DockerManager dockerManager, BenchmarkJob job, CancellationToken cancellationToken)
    {
        BenchmarkBase harness = job.HarnessFactory(dockerManager);
        string benchmarkName = BenchmarkCatalog.GetBenchmarkName(harness.Name, job.Data);
        BenchmarkResultEntry? previousResult = await resultStore.ReadPreviousResultAsync(benchmarkName, cancellationToken).ConfigureAwait(false);
        BenchmarkResult result = await harness.RunAsync(job.Data, cancellationToken).ConfigureAwait(false);
        BenchmarkResultLine resultLine = CreateResultLine(harness.Name, job.Data.Identifier, result, previousResult);

        lock (_consoleLock)
            BenchmarkConsole.WriteBenchmarkResult(resultLine);

        await resultStore.AppendResultAsync(benchmarkName, result, cancellationToken).ConfigureAwait(false);
    }

    private BenchmarkResultLine CreateResultLine(string harnessName, string dataIdentifier, BenchmarkResult result, BenchmarkResultEntry? previousResult) => new BenchmarkResultLine(
        harnessName,
        dataIdentifier,
        FormatResult(result.Min),
        FormatResult(result.Max),
        FormatResult(result.Median),
        FormatDelta(result.Median, previousResult?.Median),
        FormatResult(result.Avg),
        FormatDelta(result.Avg, previousResult?.Avg),
        "n=" + result.FilteredSampleCount.ToString(CultureInfo.InvariantCulture) + "/" + result.Samples.Length.ToString(CultureInfo.InvariantCulture) + " out=" + result.OutlierCount.ToString(CultureInfo.InvariantCulture));

    private BenchmarkResultDelta FormatDelta(double current, double? previous)
    {
        if (previous is null)
            return new BenchmarkResultDelta("n/a", null);

        if (previous.Value == 0)
            return new BenchmarkResultDelta(current == 0 ? "0%" : "n/a", null);

        double delta = ((current - previous.Value) / previous.Value) * 100;
        string text = delta.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%";
        string? style = Math.Abs(delta) < deltaWarningThresholdPercent ? null : delta < 0 ? "green" : "red";
        return new BenchmarkResultDelta(text, style);
    }

    private static string FormatResult(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private readonly record struct BenchmarkJob(Func<DockerManager, BenchmarkBase> HarnessFactory, ITestData Data);
}