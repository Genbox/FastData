using Genbox.FastData.Enums;

namespace Genbox.FastData.Config;

/// <summary>Base configuration shared by numeric and string data generation.</summary>
public abstract class DataConfig
{
    /// <summary>Gets or sets the configuration that controls which structures are available and their limits.</summary>
    public StructureConfig StructureConfig { get; set; } = StructureConfig.Default;

    /// <summary>Gets or sets the configuration that controls early-exit behavior.</summary>
    public EarlyExitConfig EarlyExitConfig { get; set; } = EarlyExitConfig.Default;

    /// <summary>Gets or sets a value indicating whether membership lookups may use an approximate Bloom filter that can return false positives.</summary>
    public bool AllowApproximation { get; set; }

    /// <summary>Gets or sets generated data-structure capabilities required by the caller.</summary>
    public StructureCapability RequiredCapability { get; set; }

    /// <summary>Gets or sets the structure type to create instead of selecting one automatically, or <see langword="null" /> or <see cref="StructureType.Auto" /> for automatic selection.</summary>
    public StructureType? StructureTypeOverride { get; set; }

    /// <summary>Gets or sets a value indicating whether data structures use the smallest possible internal data types to reduce memory.</summary>
    public bool TypeReductionEnabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether duplicate keys throw an exception.</summary>
    public bool ThrowOnDuplicates { get; set; } = true;

    /// <summary>Gets advanced settings used to fine-tune individual data structures.</summary>
    public StructureSettings StructureSettings { get; } = new StructureSettings();
}