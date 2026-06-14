using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class SubstringAnalyzerConfig
{
    public int MaxSliceByteLength { get; set; } = 8;
    public double MinUniqueFraction { get; set; } = 0.9;
    public int MaxReturned { get; set; } = 2;
}