using System.Globalization;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BenchmarkBase<T>(T bootstrap, DockerManager dockerManager) : BenchmarkBase(bootstrap, dockerManager) where T : BootstrapBase
{
    protected T Bootstrap { get; } = bootstrap;
}

public sealed record BenchmarkResult(double Min, double Median, double Max, double Avg, long FoundCount);

public abstract class BenchmarkBase : HarnessBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);
    private readonly BootstrapBase _bootstrap;

    protected BenchmarkBase(BootstrapBase bootstrap, DockerManager dockerManager) : base(bootstrap, dockerManager)
    {
        _bootstrap = bootstrap;
    }

    protected abstract string Render(ITestData data);

    public async Task<BenchmarkResult> RunAsync(ITestData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        string source = Render(data);
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        ProcessResult res;

        try
        {
            res = await base.RunAsync(source, data.Identifier, false, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Benchmark '{data.Identifier}' timed out after {Timeout}.", ex);
        }

        string[] outputLines = res.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        string output = outputLines.Length == 0 ? string.Empty : outputLines[^1].Trim();

        if (output.Length == 0)
            throw new InvalidOperationException($"Benchmark output was empty. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        string[] parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5)
            throw new InvalidOperationException($"Benchmark output was invalid: '{output}'. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        double min = double.Parse(parts[0], NumberFormatInfo.InvariantInfo);
        double median = double.Parse(parts[1], NumberFormatInfo.InvariantInfo);
        double max = double.Parse(parts[2], NumberFormatInfo.InvariantInfo);
        double avg = double.Parse(parts[3], NumberFormatInfo.InvariantInfo);
        long foundCount = long.Parse(parts[4], NumberFormatInfo.InvariantInfo);
        BenchmarkResult result = new BenchmarkResult(min, median, max, avg, foundCount);
        ValidateFoundCount(data, result);
        return result;
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
}