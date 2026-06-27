using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static class CpuAssignmentResolver
{
    public static CpuAssignment Resolve(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Cpu.CpuSet))
        {
            ValidateCpuSet(settings.Cpu.CpuSet);
            return new CpuAssignment(settings.Cpu.CpuSet, ("CPU set", settings.Cpu.CpuSet + " (configured)"));
        }

        if (!settings.Cpu.AutoSelect)
            return new CpuAssignment("0", ("CPU set", "0 (default)"));

        if (!CpuSelector.TryGetCpuSet(out string? cpuSet) || cpuSet is null)
            return new CpuAssignment("0", ("CPU set", "0 (auto unavailable)"));

        return new CpuAssignment(cpuSet, ("CPU set", cpuSet + " (auto)"));
    }

    private static void ValidateCpuSet(string cpuSet)
    {
        foreach (string part in cpuSet.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int rangeSeparator = part.IndexOf('-', StringComparison.Ordinal);

            if (rangeSeparator < 0)
            {
                ValidateCpuIndex(part, cpuSet);
                continue;
            }

            int start = ValidateCpuIndex(part[..rangeSeparator], cpuSet);
            int end = ValidateCpuIndex(part[(rangeSeparator + 1)..], cpuSet);

            if (end < start)
                throw new InvalidOperationException($"CPU set '{cpuSet}' contains an invalid range '{part}'.");
        }
    }

    private static int ValidateCpuIndex(string value, string cpuSet)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int cpu) || cpu < 0)
            throw new InvalidOperationException($"CPU set '{cpuSet}' contains invalid CPU index '{value}'.");

        return cpu;
    }
}