using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData;

[PublicAPI]
public sealed class FastDataConfig(StructureType structureType = StructureType.Auto)
{
    /// <summary>The type of structure to create. Defaults to Auto.</summary>
    public StructureType StructureType { get; set; } = structureType;

    /// <summary>When true, duplicates will be eliminated from the input.</summary>
    public DeduplicationMode DeduplicationMode { get; set; } = DeduplicationMode.HashSet;

    /// <summary>For hash-based structures, you can set this factor higher or lower to control how many slots are used. A factor higher than 1 will use more memory, but can improve performance by reducing collisions. A factor lower than 1 will use less memory, but can increase collisions and thus reduce performance.</summary>
    public int HashCapacityFactor { get; set; } = 1;

    /// <summary>Enable trimming of common prefix and suffix across string keys.</summary>
    public bool EnablePrefixSuffixTrimming { get; set; }

    /// <summary>Enable case-insensitive lookups for string keys.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Configuration for analyzers. Set to null to disable analysis.</summary>
    public StringAnalyzerConfig? StringAnalyzerConfig { get; set; } = new StringAnalyzerConfig();

    /// <summary>Maximum numeric key range (max - min) to allow BitSetStructure in Auto mode.</summary>
    public ulong BitSetStructureMaxRange { get; set; } = 4096;

    /// <summary>Minimum density (item count / range) required to use BitSetStructure in Auto mode.</summary>
    public double BitSetStructureMinDensity { get; set; } = 0.5;

    /// <summary>Minimum density required to use range checks for length maps.</summary>
    public double LengthMapMinDensity { get; set; } = 0.45;

    /// <summary>Minimum missing-bit density required to use the value bitmask early exit for numeric keys.</summary>
    public double ValueBitMaskMinDensity { get; set; } = 0.25;

    /// <summary>Minimum missing-bit density required to use the string bitmask early exit for string keys.</summary>
    public double StringBitMaskMinDensity { get; set; } = 0.25;

    /// <summary>Minimum density required to use range checks for ASCII character maps.</summary>
    public double CharMapMinDensity { get; set; } = 0.45;

    /// <summary>Minimum length density required to use KeyLengthStructure in Auto mode.</summary>
    public double KeyLengthStructureMinDensity { get; set; } = 0.75;

    /// <summary>Maximum item count to use ConditionalStructure in Auto mode.</summary>
    public int ConditionalStructureMaxItemCount { get; set; } = 400;

    /// <summary>Maximum relative slowdown allowed for a perfect hash before preferring a faster non-perfect hash.</summary>
    public double PerfectHashMaxSlowdownFactor { get; set; } = 0.25;

    /// <summary>When enabled, data structures will be generated with the smallest possible internal data types to lower memory.</summary>
    public bool TypeReductionEnabled { get; set; } = true; //TODO: Evaluate default value

    /// <summary>Enable approximate matching using a Bloom filter for primitive keys.</summary>
    public bool AllowApproximateMatching { get; set; }
}