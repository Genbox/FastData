using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators;

/// <summary>Generator configuration for early-exit-only benchmarks where no data structure is involved.</summary>
public sealed class EarlyExitOnlyGeneratorConfig(AnnotatedExpr[] earlyExits, GeneratorFunction generatorFunctions) : GeneratorConfigBase(StructureType.None, earlyExits, 0, false, StructureCapability.Membership)
{
    /// <summary>Gets the set of helper functions required by the early exit expressions.</summary>
    public GeneratorFunction GeneratorFunctions { get; } = generatorFunctions;
}