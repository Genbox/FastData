using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
public abstract class GeneratorConfigBase(StructureType structureType, AnnotatedExpr[] earlyExits, uint itemCount, bool typeReductionEnabled, StructureCapability requiredCapabilities)
{
    /// <summary>Gets the structure being generated.</summary>
    public StructureType StructureType { get; } = structureType;

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public AnnotatedExpr[] EarlyExits { get; } = earlyExits;

    /// <summary>Gets the number of keys in the dataset.</summary>
    public uint ItemCount { get; } = itemCount;

    /// <summary>Gets a value indicating whether data structures use the smallest possible fixed-width types for internal storage.</summary>
    public bool TypeReductionEnabled => typeReductionEnabled;

    /// <summary>Gets the generated data-structure capabilities required by the caller.</summary>
    public StructureCapability RequiredCapabilities { get; } = requiredCapabilities;

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; } = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);
}