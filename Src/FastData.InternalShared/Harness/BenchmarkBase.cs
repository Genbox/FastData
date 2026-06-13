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
    private const int MaxPilotIterations = 20;
    private const int AdaptiveSampleIncrement = 2;
    private const double MadScale = 1.4826d;
    private const double OutlierMadMultiplier = 3d;
    private const double OverheadCvThreshold = 0.05d;
    private const double OverheadDominanceThreshold = 0.5d;
    private const double NormalCriticalValue999 = 3.2905267314919255d;
    private static readonly double[] StudentTCriticalValues999 =
    [
        636.6192487687206d, 31.599054576942326d, 12.923978636234619d, 8.610301581379206d, 6.868826625881117d,
        5.958816178818787d, 5.407882520957641d, 5.041305433373369d, 4.780912585933472d, 4.586893858702581d,
        4.436979338234859d, 4.317791283606757d, 4.220831727531684d, 4.140454112738488d, 4.072765195903558d,
        4.014996327117081d, 3.965126272284476d, 3.921645825085222d, 3.883405852592436d, 3.849516274985267d,
        3.81927716413572d, 3.792130602882166d, 3.767626803772896d, 3.745398846283037d, 3.725143312938035d,
        3.706605311532619d, 3.689568981880842d, 3.67385050285208d, 3.6592923676194705d, 3.645758711097974d
    ];
    private static readonly TimeSpan TimeoutPerSample = TimeSpan.FromSeconds(30);

    public Action<string>? DebugOutput { get; set; }

    protected abstract string Render(ITestData data);

    public async Task<BenchmarkResult> RunAsync(ITestData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        string source = Render(data);
        WriteDebug(data, $"build start, queryCount={FormatCount(data.QueryCount)}, target={data.TargetIterationTimeMs.ToString(NumberFormatInfo.InvariantInfo)}ms, warmups={data.WarmupCount.ToString(NumberFormatInfo.InvariantInfo)}, minSamples={data.MinSampleCount.ToString(NumberFormatInfo.InvariantInfo)}, maxSamples={data.MaxSampleCount.ToString(NumberFormatInfo.InvariantInfo)}, maxError={data.MaxErrorPercent.ToString(NumberFormatInfo.InvariantInfo)}%");
        await BuildProgramAsync(source, data.Identifier, cancellationToken).ConfigureAwait(false);
        WriteDebug(data, "build complete");

        long invocations = await TuneInvocationsAsync(data, cancellationToken).ConfigureAwait(false);
        BenchmarkResult result = await MeasureAdaptiveAsync(data, invocations, cancellationToken).ConfigureAwait(false);
        ValidateFoundCount(data, result, invocations);
        return result;
    }

    private async Task<BenchmarkResult> MeasureAdaptiveAsync(ITestData data, long invocations, CancellationToken cancellationToken)
    {
        List<MeasurementSample> allSamples = [];
        int sampleCount = data.MinSampleCount;

        while (true)
        {
            MeasurementSample[] batch = await RunMeasurementAsync(data, invocations, sampleCount, cancellationToken).ConfigureAwait(false);
            allSamples.AddRange(batch);

            BenchmarkResult result = ParseResult(data, allSamples.ToArray(), invocations);
            WriteDebug(data, $"adaptive, samples={allSamples.Count.ToString(NumberFormatInfo.InvariantInfo)}/{data.MaxSampleCount.ToString(NumberFormatInfo.InvariantInfo)}, relativeError={FormatPercent(RelativeError(result))}");

            if (IsWithinTargetError(data, result))
                return result;

            if (allSamples.Count >= data.MaxSampleCount)
            {
                WriteDebug(data, $"adaptive stopped at max samples, targetError={data.MaxErrorPercent.ToString(NumberFormatInfo.InvariantInfo)}%, relativeError={FormatPercent(RelativeError(result))}");
                return result;
            }

            sampleCount = Math.Min(AdaptiveSampleIncrement, data.MaxSampleCount - allSamples.Count);
        }
    }

    private async Task<MeasurementSample[]> RunMeasurementAsync(ITestData data, long invocations, int sampleCount, CancellationToken cancellationToken)
    {
        int benchmarkIterations = checked((data.WarmupCount * 2) + (sampleCount * 2));
        TimeSpan timeout = TimeSpan.FromTicks(checked(TimeoutPerSample.Ticks * Math.Max(1, benchmarkIterations)));
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        WriteDebug(data, $"measurement run, invocations={FormatCount(invocations)}, warmups={data.WarmupCount.ToString(NumberFormatInfo.InvariantInfo)}, samples={sampleCount.ToString(NumberFormatInfo.InvariantInfo)}, timeout={timeout}");

        try
        {
            ProcessResult res = await RunProgramAsync(data.Identifier, FormatArguments(invocations, data.WarmupCount, sampleCount), timeoutSource.Token).ConfigureAwait(false);
            return ParseMeasurement(res, sampleCount);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Benchmark '{data.Identifier}' timed out after {timeout}.", ex);
        }
    }

    private async Task<long> TuneInvocationsAsync(ITestData data, CancellationToken cancellationToken)
    {
        long queryCount = data.QueryCount;
        long invocations = queryCount;
        double targetIterationTimeNs = data.TargetIterationTimeMs * 1_000_000d;
        WriteDebug(data, $"tuning start, target={FormatNs(targetIterationTimeNs)}, queryCount={FormatCount(queryCount)}");

        for (int i = 0; i < MaxPilotIterations; i++)
        {
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeoutPerSample);

            MeasurementSample[] samples;
            try
            {
                ProcessResult res = await RunProgramAsync(data.Identifier, FormatArguments(invocations, 1, 1), timeoutSource.Token).ConfigureAwait(false);
                samples = ParseMeasurement(res, 1);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Benchmark '{Name}.{data.Identifier}' pilot run timed out after {TimeoutPerSample} with {FormatCount(invocations)} invocations.", ex);
            }

            MeasurementSample pilot = samples[0];
            double correctedElapsed = pilot.Elapsed - pilot.Overhead;

            // Use corrected time for scaling when meaningful. For overhead-dominated structures
            // (e.g., RangeStructure, ConditionalStructure) where the lookup is trivial compared
            // to the key-cycling overhead, fall back to raw elapsed time.
            bool overheadDominates = correctedElapsed <= pilot.Elapsed * OverheadDominanceThreshold || correctedElapsed <= 0;
            double scalingElapsed = overheadDominates ? pilot.Elapsed : correctedElapsed;
            WriteDebug(data, $"pilot {FormatCount(i + 1)}, invocations={FormatCount(invocations)}, elapsed={FormatNs(pilot.Elapsed)}, overhead={FormatNs(pilot.Overhead)}, corrected={FormatNs(correctedElapsed)}, overheadDominates={overheadDominates}");

            if (pilot.Elapsed >= targetIterationTimeNs)
            {
                WriteDebug(data, $"tuning complete, selectedInvocations={FormatCount(invocations)}");
                return invocations;
            }

            long next = GetNextInvocationCount(data, invocations, targetIterationTimeNs, scalingElapsed);

            long minimumNext = AddInvocations(data, invocations, queryCount);
            invocations = RoundUpToMultiple(data, Math.Max(next, minimumNext), queryCount);
            WriteDebug(data, $"pilot {FormatCount(i + 1)}, nextInvocations={FormatCount(invocations)}");
        }

        throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' did not reach target iteration time {data.TargetIterationTimeMs.ToString(NumberFormatInfo.InvariantInfo)}ms after {MaxPilotIterations.ToString(NumberFormatInfo.InvariantInfo)} pilot runs.");
    }

    private BenchmarkResult ParseResult(ITestData data, MeasurementSample[] measurements, long invocations)
    {
        double[] overheads = new double[measurements.Length];
        for (int i = 0; i < measurements.Length; i++)
            overheads[i] = measurements[i].Overhead;

        bool subtractOverhead = IsOverheadStable(data, overheads);

        double[] timings = new double[measurements.Length];
        long totalFoundCount = 0;

        for (int i = 0; i < measurements.Length; i++)
        {
            MeasurementSample m = measurements[i];
            timings[i] = (subtractOverhead ? m.Elapsed - m.Overhead : m.Elapsed) / invocations;
            totalFoundCount += m.FoundCount;
        }

        double[] filteredTimings = FilterOutliers(timings);
        Array.Sort(filteredTimings);
        int outlierCount = timings.Length - filteredTimings.Length;
        WriteDebug(data, $"measurement, overheadSubtracted={subtractOverhead}, pairedOverheads=[{FormatValues(overheads)}], nsPerInvocation=[{FormatValues(timings)}]");
        WriteDebug(data, $"outliers, retained={filteredTimings.Length.ToString(NumberFormatInfo.InvariantInfo)}/{timings.Length.ToString(NumberFormatInfo.InvariantInfo)}, removed={outlierCount.ToString(NumberFormatInfo.InvariantInfo)}");
        double filteredSum = 0;

        for (int i = 0; i < filteredTimings.Length; i++)
            filteredSum += filteredTimings[i];

        double mean = filteredSum / filteredTimings.Length;
        double stdDev = StandardDeviation(filteredTimings, mean);
        double error = Error(filteredTimings.Length, stdDev);
        WriteDebug(data, $"statistics, mean={FormatNs(mean)}, error={FormatNs(error)}, stdDev={FormatNs(stdDev)}");

        return new BenchmarkResult(filteredTimings[0], filteredTimings[filteredTimings.Length / 2], filteredTimings[^1], mean, error, stdDev, totalFoundCount, timings, outlierCount);
    }

    private static MeasurementSample[] ParseMeasurement(ProcessResult res, int expectedSampleCount)
    {
        string[] outputLines = res.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        List<MeasurementSample> samples = [];
        double? pendingOverhead = null;

        foreach (string line in outputLines)
        {
            string output = line.Trim();
            string[] parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (output.StartsWith("overhead ", StringComparison.Ordinal))
            {
                if (parts.Length != 2)
                    throw new InvalidOperationException($"Benchmark overhead output was invalid: '{output}'. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

                if (pendingOverhead is not null)
                    throw new InvalidOperationException($"Benchmark output contained consecutive overhead lines without a matching sample. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

                pendingOverhead = double.Parse(parts[1], NumberFormatInfo.InvariantInfo);
                continue;
            }

            if (!output.StartsWith("sample ", StringComparison.Ordinal))
                continue;

            if (parts.Length != 3)
                throw new InvalidOperationException($"Benchmark sample output was invalid: '{output}'. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

            if (pendingOverhead is null)
                throw new InvalidOperationException($"Benchmark output contained a sample line without a preceding overhead line. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

            double elapsed = double.Parse(parts[1], NumberFormatInfo.InvariantInfo);
            long foundCount = long.Parse(parts[2], NumberFormatInfo.InvariantInfo);
            samples.Add(new MeasurementSample(pendingOverhead.Value, elapsed, foundCount));
            pendingOverhead = null;
        }

        if (samples.Count == 0)
            throw new InvalidOperationException($"Benchmark output contained no samples. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        if (samples.Count != expectedSampleCount)
            throw new InvalidOperationException($"Benchmark expected {expectedSampleCount.ToString(CultureInfo.InvariantCulture)} samples, got {samples.Count.ToString(CultureInfo.InvariantCulture)}. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        return samples.ToArray();
    }

    private bool IsOverheadStable(ITestData data, double[] overheads)
    {
        if (overheads.Length < 2)
            return true;

        double sum = 0;
        for (int i = 0; i < overheads.Length; i++)
            sum += overheads[i];

        double mean = sum / overheads.Length;

        if (mean <= double.Epsilon)
            return true;

        double stdDev = StandardDeviation(overheads, mean);
        double cv = stdDev / mean;
        WriteDebug(data, $"overhead stability, mean={FormatNs(mean)}, stdDev={FormatNs(stdDev)}, cv={FormatPercent(cv)}, stable={cv <= OverheadCvThreshold}");

        return cv <= OverheadCvThreshold;
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

    private void ValidateFoundCount(ITestData data, BenchmarkResult result, long invocations)
    {
        BenchmarkQuerySet querySet = data.GetQuerySet(bootstrap.Map);

        if (!querySet.ValidateFoundCount)
            return;

        long expectedFoundCount = checked((invocations / data.QueryCount) * querySet.ExpectedFoundCount * result.Samples.Length);
        WriteDebug(data, $"validation, expectedFoundCount={FormatCount(expectedFoundCount)}, actualFoundCount={FormatCount(result.FoundCount)}");

        if (result.FoundCount != expectedFoundCount)
            throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' expected {expectedFoundCount.ToString(CultureInfo.InvariantCulture)} matches, got {result.FoundCount.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static string FormatArguments(long invocations, int warmupCount, int sampleCount) => string.Join(' ',
        invocations.ToString(CultureInfo.InvariantCulture),
        warmupCount.ToString(CultureInfo.InvariantCulture),
        sampleCount.ToString(CultureInfo.InvariantCulture));

    private long GetNextInvocationCount(ITestData data, long invocations, double targetIterationTimeNs, double elapsed)
    {
        if (elapsed <= 0d)
        {
            if (invocations > long.MaxValue / 2)
                throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' requires too many invocations to produce a positive pilot measurement.");

            return invocations * 2;
        }

        double scaled = Math.Ceiling(invocations * targetIterationTimeNs / elapsed);
        if (!double.IsFinite(scaled) || scaled > long.MaxValue)
            throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' requires too many invocations to reach target iteration time {data.TargetIterationTimeMs.ToString(NumberFormatInfo.InvariantInfo)}ms.");

        return (long)scaled;
    }

    private long RoundUpToMultiple(ITestData data, long value, long multiple)
    {
        long remainder = value % multiple;
        if (remainder == 0)
            return value;

        long increment = multiple - remainder;
        if (value > long.MaxValue - increment)
            throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' requires too many invocations to align with query count {multiple.ToString(NumberFormatInfo.InvariantInfo)}.");

        return value + increment;
    }

    private long AddInvocations(ITestData data, long invocations, long queryCount)
    {
        if (invocations > long.MaxValue - queryCount)
            throw new InvalidOperationException($"Benchmark '{Name}.{data.Identifier}' requires too many invocations to reach target iteration time {data.TargetIterationTimeMs.ToString(NumberFormatInfo.InvariantInfo)}ms.");

        return invocations + queryCount;
    }

    private static double StandardDeviation(double[] values, double mean)
    {
        if (values.Length < 2)
            return 0d;

        double sumOfSquares = 0d;

        for (int i = 0; i < values.Length; i++)
        {
            double delta = values[i] - mean;
            sumOfSquares += delta * delta;
        }

        return Math.Sqrt(sumOfSquares / (values.Length - 1));
    }

    private static double Error(int sampleCount, double stdDev)
    {
        if (sampleCount < 2 || stdDev <= double.Epsilon)
            return 0d;

        int degreesOfFreedom = sampleCount - 1;
        double criticalValue = degreesOfFreedom <= StudentTCriticalValues999.Length ? StudentTCriticalValues999[degreesOfFreedom - 1] : NormalCriticalValue999;
        return criticalValue * stdDev / Math.Sqrt(sampleCount);
    }

    private static bool IsWithinTargetError(ITestData data, BenchmarkResult result) => RelativeError(result) <= data.MaxErrorPercent / 100d;

    private static double RelativeError(BenchmarkResult result) => result.Avg <= double.Epsilon ? double.PositiveInfinity : result.Error / result.Avg;

    private void WriteDebug(ITestData data, string message) => DebugOutput?.Invoke($"{Name}.{data.Identifier}: {message}");

    private static string FormatCount(long value) => value.ToString("N0", NumberFormatInfo.InvariantInfo);

    private static string FormatNs(double value) => value.ToString("0.##", NumberFormatInfo.InvariantInfo) + " ns";

    private static string FormatPercent(double value) => (value * 100d).ToString("0.##", NumberFormatInfo.InvariantInfo) + "%";

    private static string FormatValues(double[] values)
    {
        string[] formatted = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
            formatted[i] = FormatNs(values[i]);

        return string.Join(", ", formatted);
    }

    private sealed record MeasurementSample(double Overhead, double Elapsed, long FoundCount);
}