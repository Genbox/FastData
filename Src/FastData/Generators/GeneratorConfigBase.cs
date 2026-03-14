using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
public abstract class GeneratorConfigBase(string structureName, IEarlyExit[] earlyExits, uint itemCount, bool typeReductionEnabled)
{
    /// <summary>Name of the structure being generated.</summary>
    public string StructureName { get; } = structureName.Substring(0, structureName.Length - 9);

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public IEarlyExit[] EarlyExits { get; } = earlyExits;

    public uint ItemCount { get; } = itemCount;

    public bool TypeReductionEnabled => typeReductionEnabled;

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; } = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);
}