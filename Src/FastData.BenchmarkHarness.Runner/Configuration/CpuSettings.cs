namespace Genbox.FastData.BenchmarkHarness.Runner.Configuration;

internal sealed class CpuSettings
{
    public bool AutoSelect { get; set; } = true;
    public string? CpuSet { get; set; }
}