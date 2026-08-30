using Genbox.FastData.Enums;

namespace Genbox.FastData;

/// <summary>Represents generated source code and metadata for a string-key lookup.</summary>
/// <param name="Source">The generated source code.</param>
/// <param name="EarlyExits">The names of early-exit strategies included in the generated lookup.</param>
/// <param name="StringHashName">The selected string-hash strategy name, or <see langword="null" /> when no specialized strategy was selected.</param>
/// <param name="StructureType">The data structure selected for the generated lookup.</param>
public readonly record struct StringGenerationResult(string Source, string[] EarlyExits, string? StringHashName, StructureType StructureType);