using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.InternalShared.Helpers;

public static class ProcessHelper
{
    public static bool TryRunProcess(string application, string args)
    {
        try
        {
            return RunProcess(application, args).ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static int RunShell(string application, string? args = null, string? workingDir = null, int timeoutMs = 60_000, nint affinity = -1)
    {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = application,
            Arguments = args,
            CreateNoWindow = false,
            UseShellExecute = true,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        process.Start();

        if (affinity >= 0)
            process.ProcessorAffinity = affinity;

        bool exited = process.WaitForExit(timeoutMs);

        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore - best effort.
            }

            try
            {
                process.WaitForExit(1000);
            }
            catch
            {
                // Ignore - best effort.
            }
        }

        return process.ExitCode;
    }

    [SuppressMessage("Usage", "MA0040:Forward the CancellationToken parameter to methods that take one")]
    public static ProcessResult RunProcess(string application, string? args = null, string? workingDir = null, int timeoutMs = 5000, nint affinity = -1)
    {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = application,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.Start();

        if (affinity >= 0)
            process.ProcessorAffinity = affinity;

        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(timeoutMs);

        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore - best effort.
            }

            try
            {
                process.WaitForExit(1000);
            }
            catch
            {
                // Ignore - best effort.
            }
        }

        string stdOut = "";
        string stdErr = "";

        try { stdOut = stdOutTask.GetAwaiter().GetResult(); }
        catch
        {
            /* best effort */
        }
        try { stdErr = stdErrTask.GetAwaiter().GetResult(); }
        catch
        {
            /* best effort */
        }

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }
}