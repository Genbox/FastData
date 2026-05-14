using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Internal.Extensions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for numeric code generators in the FastData library.</summary>
public sealed class NumericGeneratorConfig : GeneratorConfigBase
{
    internal NumericGeneratorConfig(Type structureType, uint itemCount, object minValue, object maxValue, AnnotatedExpr[] earlyExits, bool typeReductionEnabled, bool hasZero) : base(structureType.GetFriendlyName(), earlyExits, itemCount, typeReductionEnabled)
    {
        Constants = new NumericConstants(minValue, maxValue);
        HasZero = hasZero;
    }

    /// <summary>Gets numeric metadata constants emitted with the generated structure.</summary>
    public NumericConstants Constants { get; }

    /// <summary>Gets a value indicating whether the input data contains zero.</summary>
    public bool HasZero { get; }
}