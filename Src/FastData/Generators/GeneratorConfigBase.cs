using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
public abstract class GeneratorConfigBase(string structureName, AnnotatedExpr[] earlyExits, uint itemCount, bool typeReductionEnabled)
{
    /// <summary>Name of the structure being generated.</summary>
    public string StructureName { get; } = structureName;

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public AnnotatedExpr[] EarlyExits { get; } = earlyExits;

    /// <summary>The number of keys in the dataset</summary>
    public uint ItemCount { get; } = itemCount;

    /// <summary>When enabled, data structures uses the smallest possible fixed-width data types for internal structures</summary>
    public bool TypeReductionEnabled => typeReductionEnabled;

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; } = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);
}