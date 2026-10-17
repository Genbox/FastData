using Genbox.FastData.Config.Analysis;

namespace Genbox.FastData.Config;

/// <summary>Configuration for generating string-key lookup structures.</summary>
public sealed class StringDataConfig : DataConfig
{
    /// <summary>Initializes a new instance with the default string structure settings.</summary>
    public StringDataConfig()
    {
        StructureSettings.AddDefault(KnownSettings.HashTableCapacityFactor, 1f);
        StructureSettings.AddDefault(KnownSettings.OptimizeHashTableBucketSize, true);
        StructureSettings.AddDefault(KnownSettings.RoundModuloToPowerOfTwo, true);
        StructureSettings.AddDefault(KnownSettings.RoundModuloToPowerOfTwoThreshold, 0.125f);
    }

    /// <summary>Gets or sets a value indicating whether ASCII string keys are compared case-insensitively.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Gets or sets the string-hash analyzer configuration, or <see langword="null" /> to disable analysis.</summary>
    public StringAnalyzerConfig? StringAnalyzerConfig { get; set; }
}