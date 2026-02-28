using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
public abstract class GeneratorConfigBase(Type structureType, HashDetails hashDetails, FastDataConfig cfg, uint itemCount, IEarlyExit[] earlyExits)
{
    /// <summary>Gets the structure type that the generator will create.</summary>
    public Type StructureType { get; } = structureType;

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public IEarlyExit[] EarlyExits { get; } = earlyExits;

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; } = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);

    /// <summary>Contains information about the hash function to use.</summary>
    public HashDetails HashDetails { get; } = hashDetails;

    public uint ItemCount { get; } = itemCount;

    public bool TypeReductionEnabled => cfg.TypeReductionEnabled;
}