using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class SegmentGeneratorConfig
{
    /// <summary>Options for delta-map based segment generation.</summary>
    public DeltaGeneratorConfig? DeltaGeneratorConfig { get; set; } = new DeltaGeneratorConfig();

    /// <summary>Options for exhaustive prefix and suffix segment generation.</summary>
    public BruteForceGeneratorConfig? BruteForceGeneratorConfig { get; set; } = new BruteForceGeneratorConfig();

    /// <summary>Options for prefix and suffix edge-gram segment generation.</summary>
    public EdgeGramGeneratorConfig? EdgeGramGeneratorConfig { get; set; } = new EdgeGramGeneratorConfig();

    /// <summary>Options for full-tail segment generation from every left-aligned offset.</summary>
    public OffsetGeneratorConfig? OffsetGeneratorConfig { get; set; } = new OffsetGeneratorConfig();
}