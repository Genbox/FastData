using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class PositionLengthAnalyzerConfig : IAnalyzerConfig
{
    public bool IncludeLength { get; set; } = true;
    public bool IncludeLastChar { get; set; } = true;
}