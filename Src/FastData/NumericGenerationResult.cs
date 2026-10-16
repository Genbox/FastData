using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData;

/// <summary>Represents generated source code and metadata for a numeric-key lookup.</summary>
/// <param name="Source">The generated source code.</param>
/// <param name="EarlyExits">The names of early-exit strategies included in the generated lookup.</param>
/// <param name="StructureType">The data structure selected for the generated lookup.</param>
[PublicAPI]
public readonly record struct NumericGenerationResult(string Source, string[] EarlyExits, StructureType StructureType);