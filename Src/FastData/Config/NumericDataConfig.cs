namespace Genbox.FastData.Config;

/// <summary>Configuration for generating numeric-key lookup structures.</summary>
public sealed class NumericDataConfig : DataConfig
{
    public NumericDataConfig()
    {
        StructureSettings.AddDefault(KnownSettings.HashTableCapacityFactor, 1f);
        StructureSettings.AddDefault(KnownSettings.EliasFanoSkipQuantum, 128);
        StructureSettings.AddDefault(KnownSettings.PgmEpsilon, 64);
        StructureSettings.AddDefault(KnownSettings.PgmEpsilonRecursive, 4);
    }
}