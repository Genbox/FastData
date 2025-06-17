using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData;

[PublicAPI]
public sealed class FastDataConfig(StructureType structureType = StructureType.Auto)
{
    /// <summary>The type of structure to create. Defaults to Auto.</summary>
    public StructureType StructureType { get; set; } = structureType;

    /// <summary>For hash-based structures, you can set this factor higher or lower to control how many slots are used. A factor higher than 1 will use more memory, but can improve performance by reducing collisions. A factor lower than 1 will use less memory, but can increase collisions and thus reduce performance.</summary>
    public int HashCapacityFactor { get; set; } = 1;

    /// <summary>Configuration for analyzers. Set to null to disable analysis.</summary>
    public StringAnalyzerConfig? StringAnalyzerConfig { get; set; } = new StringAnalyzerConfig();
}