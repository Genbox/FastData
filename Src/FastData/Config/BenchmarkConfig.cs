namespace Genbox.FastData.Config;

public abstract class BenchmarkConfig
{
    /// <summary>Maximum relative slowdown allowed for a perfect hash before preferring a faster non-perfect hash.</summary>
    public double PerfectHashMaxSlowdownFactor { get; set; } = 0.25;
}