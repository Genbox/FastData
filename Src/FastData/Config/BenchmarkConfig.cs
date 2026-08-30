namespace Genbox.FastData.Config;

/// <summary>Provides common options for benchmark-based candidate selection.</summary>
public abstract class BenchmarkConfig
{
    /// <summary>Maximum relative slowdown allowed for a perfect hash before preferring a faster non-perfect hash.</summary>
    public double PerfectHashMaxSlowdownFactor { get; set; } = 0.25;
}