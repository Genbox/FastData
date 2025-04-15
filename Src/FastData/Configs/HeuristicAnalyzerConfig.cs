using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Configs;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public sealed class HeuristicAnalyzerConfig : IAnalyzerConfig
{
    public uint MaxPositions { get; set; } = 255;
}