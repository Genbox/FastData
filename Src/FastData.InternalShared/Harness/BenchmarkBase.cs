using System.Globalization;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BenchmarkBase<T>(T bootstrap, DockerManager dockerManager) : BenchmarkBase(bootstrap, dockerManager) where T : BootstrapBase
{
    protected T Bootstrap { get; } = bootstrap;
}

public sealed record BenchmarkResult(double Min, double Median, double Max, double Avg);

public abstract class BenchmarkBase(BootstrapBase bootstrap, DockerManager dockerManager) : HarnessBase(bootstrap, dockerManager)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    protected abstract string Render(ITestData data);

    public async Task<BenchmarkResult> RunAsync(ITestData data, CancellationToken cancellationToken = default)
    {
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

        if (parts.Length != 4)
            throw new InvalidOperationException($"Benchmark output was invalid: '{output}'. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        double min = double.Parse(parts[0], NumberFormatInfo.InvariantInfo);
        double median = double.Parse(parts[1], NumberFormatInfo.InvariantInfo);
        double max = double.Parse(parts[2], NumberFormatInfo.InvariantInfo);
        double avg = double.Parse(parts[3], NumberFormatInfo.InvariantInfo);
        return new BenchmarkResult(min, median, max, avg);
    }
}