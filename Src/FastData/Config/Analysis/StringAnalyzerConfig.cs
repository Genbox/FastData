using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

/// <summary>Configures string-hash analysis.</summary>
[PublicAPI]
public sealed class StringAnalyzerConfig
{
    /// <summary>Gets the minimum relative speed advantage required before a non-perfect hash is preferred to a perfect hash.</summary>
    public double PerfectHashThreshold { get; } = 0.25; // 25%

    /// <summary>Gets or sets the number of iterations used to benchmark each string-hash candidate. Set to <c>0</c> to select by fitness without benchmarking.</summary>
    public int BenchmarkIterations { get; set; } = 1000;

    /// <summary>Gets or sets position-and-length analyzer options, or <see langword="null" /> to disable that analyzer.</summary>
    public PositionLengthAnalyzerConfig? PositionLengthAnalyzerConfig { get; set; } = new PositionLengthAnalyzerConfig();

    /// <summary>Gets or sets brute-force analyzer options, or <see langword="null" /> to disable that analyzer.</summary>
    public BruteForceAnalyzerConfig? BruteForceAnalyzerConfig { get; set; } = new BruteForceAnalyzerConfig();

    /// <summary>Gets or sets genetic analyzer options, or <see langword="null" /> to disable that analyzer.</summary>
    public GeneticAnalyzerConfig? GeneticAnalyzerConfig { get; set; } = new GeneticAnalyzerConfig();

    /// <summary>Gets or sets gperf analyzer options, or <see langword="null" /> to disable that analyzer.</summary>
    public GPerfAnalyzerConfig? GPerfAnalyzerConfig { get; set; } = new GPerfAnalyzerConfig();

    /// <summary>Gets or sets the segment-generation options shared by string-hash analyzers.</summary>
    public SegmentGeneratorConfig SegmentGeneratorConfig { get; set; } = new SegmentGeneratorConfig();
}