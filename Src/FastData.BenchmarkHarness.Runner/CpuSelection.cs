using System.Globalization;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal sealed class CpuSelection(int logicalProcessor, int physicalCoreIndex, int siblings, int logicalProcessorCount, int physicalCoreCount)
{
    public int LogicalProcessor { get; } = logicalProcessor;
    public int PhysicalCoreIndex { get; } = physicalCoreIndex;
    public int Siblings { get; } = siblings;
    public int LogicalProcessorCount { get; } = logicalProcessorCount;
    public int PhysicalCoreCount { get; } = physicalCoreCount;
    public string CpuSet => LogicalProcessor.ToString(CultureInfo.InvariantCulture);
}