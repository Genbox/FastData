using System.Diagnostics;
using System.Globalization;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static class ThermalStabilization
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(2);

    public static void Run(string cpuSet, Action<string>? debugOutput)
    {
        int cpuIndex = ParseFirstCpuIndex(cpuSet);
        IntPtr affinityMask = new IntPtr(1L << cpuIndex);

        debugOutput?.Invoke($"thermal stabilization start, cpu={cpuIndex.ToString(CultureInfo.InvariantCulture)}, duration={Duration}");

        Process process = Process.GetCurrentProcess();
        IntPtr previousAffinity;

        try
        {
            previousAffinity = process.ProcessorAffinity;
        }
        catch (PlatformNotSupportedException)
        {
            // macOS does not support ProcessorAffinity. Run unaffinized.
            RunSpinLoop(Duration);
            debugOutput?.Invoke("thermal stabilization complete (no affinity support)");
            return;
        }

        try
        {
            process.ProcessorAffinity = affinityMask;
            RunSpinLoop(Duration);
        }
        finally
        {
            try
            {
                process.ProcessorAffinity = previousAffinity;
            }
            catch (PlatformNotSupportedException)
            {
                // Should not happen if the initial read succeeded, but guard anyway.
            }
        }

        debugOutput?.Invoke($"thermal stabilization complete, cpu={cpuIndex.ToString(CultureInfo.InvariantCulture)}");
    }

    private static void RunSpinLoop(TimeSpan duration)
    {
        // Tight arithmetic loop to saturate the CPU core. The volatile read
        // prevents the compiler from optimizing away the computation.
        long deadline = Stopwatch.GetTimestamp() + (long)(duration.TotalSeconds * Stopwatch.Frequency);
        uint accumulator = 0;

        while (Stopwatch.GetTimestamp() < deadline)
        {
            for (int i = 0; i < 1000; i++)
                accumulator = (accumulator * 2654435761u) + 1;
        }

        // Prevent dead-code elimination of the loop.
        if (accumulator == uint.MaxValue)
            throw new InvalidOperationException("Unreachable");
    }

    private static int ParseFirstCpuIndex(string cpuSet)
    {
        // Extract the first integer from a Docker CPU set string like "4", "2,4,6", or "2-6".
        ReadOnlySpan<char> span = cpuSet.AsSpan().Trim();

        int end = 0;
        while (end < span.Length && char.IsAsciiDigit(span[end]))
            end++;

        if (end > 0 && int.TryParse(span[..end], NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            return index;

        return 0;
    }
}