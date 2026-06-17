using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class SubstringAnalyzerConfig
{
    /// <summary>Maximum bytes to inspect for one substring candidate. The default covers up to eight UTF-16 code units.</summary>
    public int MaxSliceByteLength { get; set; } = 16;
    public double MinUniqueFraction { get; set; } = 0.9;
    public int MaxReturned { get; set; } = 2;
}