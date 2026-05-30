namespace Genbox.FastData.Config;

/// <summary>Base configuration shared by numeric and string data generation.</summary>
public abstract class DataConfig
{
    /// <summary>Configuration controlling which structures are available and their limits.</summary>
    public StructureConfig StructureConfig { get; set; } = StructureConfig.Default;

    /// <summary>Configuration controlling early-exit behavior.</summary>
    public EarlyExitConfig EarlyExitConfig { get; set; } = EarlyExitConfig.Default;

    /// <summary>Enable approximate matching using a Bloom filter.</summary>
    public bool AllowApproximation { get; set; }

    /// <summary>Override the type of structure to create. Mostly used for internal testing.</summary>
    public Type? StructureTypeOverride { get; set; }

    /// <summary>When enabled, data structures will be generated with the smallest possible internal data types to lower memory.</summary>
    public bool TypeReductionEnabled { get; set; } = true;

    /// <summary>When true, throws an exception on duplicate keys</summary>
    public bool ThrowOnDuplicates { get; set; } = true;

    /// <summary>Can be used to give advanced fine-tuning parameters to individual data structures</summary>
    public StructureSettings StructureSettings { get; } = new StructureSettings();
}