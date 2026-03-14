using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Extensions;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for numeric code generators in the FastData library.</summary>
/// <typeparam name="TKey">The numeric key type.</typeparam>
public sealed class NumericGeneratorConfig<TKey> : GeneratorConfigBase
{
    internal NumericGeneratorConfig(Type structureType, uint itemCount, TKey minValue, TKey maxValue, IEarlyExit[] earlyExits, bool typeReductionEnabled, bool hasZeroOrNaN) : base(structureType.GetFriendlyName(), earlyExits, itemCount, typeReductionEnabled)
    {
        NumericConstants<TKey> constants = new NumericConstants<TKey>();
        constants.MinValue = minValue;
        constants.MaxValue = maxValue;
        Constants = constants;

        HasZeroOrNaN = hasZeroOrNaN;
    }

    public NumericConstants<TKey> Constants { get; }

    public bool HasZeroOrNaN { get; }
}