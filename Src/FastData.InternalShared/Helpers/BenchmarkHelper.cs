using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.InternalShared.Helpers;

public static class BenchmarkHelper
{
    private static int _cpuCounter;
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(10);

    public static void RunBenchmark(string program, string args, string workingDir, string bencherArgs, bool useBencher, bool useShell)
    {
        ProcessResult res;

        // If shell is activated we rewrite the cmdline to execute in a cmd and pause at the end
        if (useShell)
        {
            args = $" /c {program} {args} && pause";
            program = "cmd.exe";
        }

        // We use affinity to ensure two benchmarks don't share a CPU (if possible)
        Interlocked.Increment(ref _cpuCounter);

        if (_cpuCounter >= Environment.ProcessorCount)
            _cpuCounter = 0;

        if (useBencher)
        {
            if (!ProcessHelper.TryRunProcess("bencher", "--version"))
                throw new InvalidOperationException("Bencher is not available");

            if (Environment.GetEnvironmentVariable("BENCHER_API_TOKEN") == null)
                throw new InvalidOperationException("BENCHER_API_TOKEN must be set");

            res = Run(useShell, "bencher", $"run {bencherArgs} \"{program} {args}\"", workingDir);
        }
        else
            res = Run(useShell, program, args, workingDir);

        if (res.ExitCode != 0)
            throw new InvalidOperationException($"Failed to run benchmarks. Return code: {res.ExitCode}\nSTDOUT:\n{res.StandardOutput}\nSTDERR:\n{res.StandardError}");
    }

    private static ProcessResult Run(bool useShell, string application, string args, string workingDir)
    {
        // calculate the affinity mask
        long mask = 1L << _cpuCounter;

        // Affinity will likely not work when useShell is true. It is a best effort.
        if (useShell)
        {
            int code = ProcessHelper.RunShell(application, args, workingDir, (int)_timeout.TotalMilliseconds, (nint)mask);
            return new ProcessResult(code, string.Empty, string.Empty);
        }

        return ProcessHelper.RunProcess(application, args, workingDir, (int)_timeout.TotalMilliseconds, (nint)mask);
    }
}