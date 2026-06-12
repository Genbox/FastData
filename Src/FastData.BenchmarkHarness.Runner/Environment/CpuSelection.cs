using System.Globalization;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal sealed class CpuSelection(int logicalProcessor)
{
    public int LogicalProcessor { get; } = logicalProcessor;
    public string CpuSet => LogicalProcessor.ToString(CultureInfo.InvariantCulture);
}