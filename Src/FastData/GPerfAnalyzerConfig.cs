using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData;

[PublicAPI]
public sealed class GPerfAnalyzerConfig : IAnalyzerConfig
{
    public uint MaxPositions { get; set; } = 255;
}