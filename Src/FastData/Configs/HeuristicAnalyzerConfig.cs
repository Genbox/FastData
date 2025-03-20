using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Configs;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public sealed class HeuristicAnalyzerConfig : AnalyzerConfig
{
    public HeuristicAnalyzerConfig() => TimeWeight = 0;
}