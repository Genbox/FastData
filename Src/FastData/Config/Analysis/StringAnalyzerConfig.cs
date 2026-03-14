using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class StringAnalyzerConfig
{
    public double PerfectHashThreshold { get; } = 0.25; // 25%
    public int BenchmarkIterations { get; set; } = 1000;
    public BruteForceAnalyzerConfig? BruteForceAnalyzerConfig { get; set; } = new BruteForceAnalyzerConfig();
    public GeneticAnalyzerConfig? GeneticAnalyzerConfig { get; set; } = new GeneticAnalyzerConfig();
    public GPerfAnalyzerConfig? GPerfAnalyzerConfig { get; set; } = new GPerfAnalyzerConfig();
}