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
    protected abstract string Render(ITestData data);

    public async Task<double> RunAsync(ITestData data, CancellationToken cancellationToken = default)
    {
        string source = Render(data);
        ProcessResult res = await base.RunAsync(source, data.Identifier, false, cancellationToken).ConfigureAwait(false);
        string output = res.StandardOutput.Trim();

        if (output.Length == 0)
            throw new InvalidOperationException($"Benchmark output was empty. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        return double.Parse(output, NumberFormatInfo.InvariantInfo);
    }
}