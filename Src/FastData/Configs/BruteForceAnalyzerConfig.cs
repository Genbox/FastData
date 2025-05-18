using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Configs;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public sealed class BruteForceAnalyzerConfig : IAnalyzerConfig
{
    public double MinFitness { get; set; } = 0.9;
    public int MaxAttempts { get; set; } = 1_000_000;
    public int MaxReturned { get; set; } = 10;
}