namespace Genbox.FastData.Config;

/// <summary>Base configuration shared by numeric and string data generation.</summary>
public abstract class DataConfig
{
    /// <summary>Configuration controlling which structures are available and their limits.</summary>
    public StructureConfig StructureConfig { get; set; } = StructureConfig.Default;

    /// <summary>Configuration controlling early-exit behavior.</summary>
    public EarlyExitConfig EarlyExitConfig { get; set; } = EarlyExitConfig.Default;

    /// <summary>Enable approximate matching using a Bloom filter.</summary>
    public bool AllowApproximateMatching { get; set; }

    /// <summary>Override the type of structure to create. Mostly used for internal testing.</summary>
    public Type? StructureTypeOverride { get; set; }

    /// <summary>When enabled, data structures will be generated with the smallest possible internal data types to lower memory.</summary>
    public bool TypeReductionEnabled { get; set; } = true;

    /// <summary>When true, throws an exception on duplicate keys</summary>
    public bool ThrowOnDuplicates { get; set; } = true;

    /// <summary>
    /// For hash-based structures, you can set this factor higher or lower to control how many slots are used. A factor higher than 1 will use more memory, but can improve performance by reducing collisions. A factor lower than 1 will use less memory, but can increase collisions and thus reduce
    /// performance.
    /// </summary>
    public int HashCapacityFactor { get; set; } = 1;

    /// <summary>Can be used to give advanced fine-tuning parameters to individual data structures</summary>
    public Dictionary<string, object> StructureSettings { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}