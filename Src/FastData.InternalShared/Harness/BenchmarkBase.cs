using System.Globalization;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BenchmarkBase<T>(T bootstrap, DockerManager dockerManager) : BenchmarkBase(bootstrap, dockerManager) where T : BootstrapBase
{
    protected T Bootstrap { get; } = bootstrap;
}

public abstract class BenchmarkBase(BootstrapBase bootstrap, DockerManager dockerManager) : HarnessBase(bootstrap, dockerManager)
{
    private const int MinSamplesForOutlierFiltering = 7;
    private const double MadScale = 1.4826d;
    private const double OutlierMadMultiplier = 3d;
    private static readonly TimeSpan TimeoutPerSample = TimeSpan.FromSeconds(30);
    private readonly BootstrapBase _bootstrap = bootstrap;

    protected abstract string Render(ITestData data);

    public async Task<BenchmarkResult> RunAsync(ITestData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        string source = Render(data);
        TimeSpan timeout = TimeSpan.FromTicks(checked(TimeoutPerSample.Ticks * Math.Max(1, data.SampleCount)));
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        ProcessResult res;

        try
        {
            res = await base.RunAsync(source, data.Identifier, false, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Benchmark '{data.Identifier}' timed out after {timeout}.", ex);
        }

        BenchmarkResult result = ParseResult(data, res);
        ValidateFoundCount(data, result);
        return result;
    }

    private static BenchmarkResult ParseResult(ITestData data, ProcessResult res)
    {
        string[] outputLines = res.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        List<BenchmarkSample> samples = [];

        foreach (string line in outputLines)
        {
            string output = line.Trim();

            if (!output.StartsWith("sample ", StringComparison.Ordinal))
                continue;

            string[] parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                throw new InvalidOperationException($"Benchmark sample output was invalid: '{output}'. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

            double elapsed = double.Parse(parts[1], NumberFormatInfo.InvariantInfo);
            long foundCount = long.Parse(parts[2], NumberFormatInfo.InvariantInfo);
            samples.Add(new BenchmarkSample(elapsed, foundCount));
        }

        if (samples.Count == 0)
            throw new InvalidOperationException($"Benchmark output contained no samples. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        if (samples.Count != data.SampleCount)
            throw new InvalidOperationException($"Benchmark expected {data.SampleCount.ToString(CultureInfo.InvariantCulture)} samples, got {samples.Count.ToString(CultureInfo.InvariantCulture)}. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        double[] timings = new double[samples.Count];
        long totalFoundCount = 0;

        for (int i = 0; i < samples.Count; i++)
        {
            BenchmarkSample sample = samples[i];
            timings[i] = sample.Elapsed;
            totalFoundCount += sample.FoundCount;
        }

        double[] filteredTimings = FilterOutliers(timings);
        Array.Sort(filteredTimings);
        int outlierCount = timings.Length - filteredTimings.Length;
        double filteredSum = 0;

        for (int i = 0; i < filteredTimings.Length; i++)
            filteredSum += filteredTimings[i];

        return new BenchmarkResult(filteredTimings[0], filteredTimings[filteredTimings.Length / 2], filteredTimings[^1], filteredSum / filteredTimings.Length, totalFoundCount, timings, filteredTimings.Length, outlierCount);
    }

    private static double[] FilterOutliers(double[] timings)
    {
        if (timings.Length < MinSamplesForOutlierFiltering)
            return (double[])timings.Clone();

        double[] sortedTimings = (double[])timings.Clone();
        Array.Sort(sortedTimings);
        double median = sortedTimings[sortedTimings.Length / 2];
        double[] deviations = new double[timings.Length];

        for (int i = 0; i < timings.Length; i++)
            deviations[i] = Math.Abs(timings[i] - median);

        Array.Sort(deviations);
        double mad = deviations[deviations.Length / 2];

        if (mad < double.Epsilon)
            return FilterZeroMadOutliers(timings, sortedTimings, median);

        double threshold = OutlierMadMultiplier * MadScale * mad;
        double lowerBound = median - threshold;
        double upperBound = median + threshold;
        List<double> filtered = [];

        for (int i = 0; i < timings.Length; i++)
        {
            double timing = timings[i];

            if (timing >= lowerBound && timing <= upperBound)
                filtered.Add(timing);
        }

        int minRetainedSamples = (int)Math.Ceiling(timings.Length * 2d / 3d);
        if (filtered.Count < minRetainedSamples)
            return sortedTimings;

        return filtered.ToArray();
    }

    private static double[] FilterZeroMadOutliers(double[] timings, double[] sortedTimings, double median)
    {
        List<double> filtered = [];

        for (int i = 0; i < timings.Length; i++)
        {
            double timing = timings[i];

            if (Math.Abs(timing - median) <= double.Epsilon)
                filtered.Add(timing);
        }

        int minRetainedSamples = (int)Math.Ceiling(timings.Length * 2d / 3d);
        if (filtered.Count < minRetainedSamples)
            return sortedTimings;

        return filtered.ToArray();
    }

    private void ValidateFoundCount(ITestData data, BenchmarkResult result)
    {
        BenchmarkQuerySet querySet = data.GetQuerySet(_bootstrap.Map);

        if (!querySet.ValidateFoundCount)
            return;

        long expectedFoundCount = checked((long)data.WorkIterations * querySet.ExpectedFoundCount * data.SampleCount);

        if (result.FoundCount != expectedFoundCount)
            throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' expected {expectedFoundCount.ToString(CultureInfo.InvariantCulture)} matches, got {result.FoundCount.ToString(CultureInfo.InvariantCulture)}.");
    }

    private sealed record BenchmarkSample(double Elapsed, long FoundCount);
}