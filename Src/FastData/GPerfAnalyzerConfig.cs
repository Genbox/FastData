using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
internal sealed class GPerfAnalyzerConfig : IAnalyzerConfig
{
    public uint MaxPositions { get; set; } = 255;
}