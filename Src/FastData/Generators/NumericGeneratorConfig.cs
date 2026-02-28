using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for numeric code generators in the FastData library.</summary>
/// <typeparam name="TKey">The numeric key type.</typeparam>
public sealed class NumericGeneratorConfig<TKey> : GeneratorConfigBase
{
    internal NumericGeneratorConfig(Type structureType, uint itemCount, NumericKeyProperties<TKey> props, HashDetails hashDetails, FastDataConfig cfg) : base(structureType, hashDetails, cfg, itemCount, GetEarlyExits(props, itemCount, cfg, structureType).ToArray())
    {
        Constants = CreateConstants(props);
    }

    public NumericConstants<TKey> Constants { get; }

    private static NumericConstants<TKey> CreateConstants(NumericKeyProperties<TKey> props)
    {
        NumericConstants<TKey> constants = new NumericConstants<TKey>();
        constants.MinValue = props.MinKeyValue;
        constants.MaxValue = props.MaxKeyValue;
        return constants;
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(NumericKeyProperties<TKey> props, uint itemCount, FastDataConfig cfg, Type structureType)
    {
        //There is no point to using early exists if there is just one item
        if (itemCount == 1)
            yield break;

        //These don't support early exits
        if (structureType == typeof(SingleValueStructure<,>) || structureType == typeof(BitSetStructure<,>) || structureType == typeof(RangeStructure<,>))
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == typeof(ConditionalStructure<,>) && itemCount <= 3)
            yield break;

        //If the min and max keys are equal, the generator can output a single length check conditional, which is better than any other method.
        if (props.MinKeyValue.Equals(props.MaxKeyValue))
            yield return new ValueRangeEarlyExit<TKey>(props.MinKeyValue, props.MaxKeyValue); // 1 op: val.Len != len
        else if (IsBitMaskViable(props, cfg, out ulong mask))
            yield return new ValueBitMaskEarlyExit(mask); // 2 ops: val & mask != 0
        else
            yield return new ValueRangeEarlyExit<TKey>(props.MinKeyValue, props.MaxKeyValue); // 3 ops: len < min || len > max
    }

    private static bool IsBitMaskViable(NumericKeyProperties<TKey> props, FastDataConfig cfg, out ulong mask)
    {
        Type keyType = typeof(TKey);

        ulong fullMask = Type.GetTypeCode(keyType) switch
        {
            TypeCode.SByte => byte.MaxValue,
            TypeCode.Byte => byte.MaxValue,
            TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Char => ushort.MaxValue,
            TypeCode.Int32 => uint.MaxValue,
            TypeCode.UInt32 => uint.MaxValue,
            TypeCode.Int64 => ulong.MaxValue,
            TypeCode.UInt64 => ulong.MaxValue,
            _ => 0 // We don't support masks for float/double
        };

        if (fullMask == 0 || props.BitMask == fullMask)
        {
            mask = 0;
            return false;
        }

        mask = fullMask ^ props.BitMask; // Invert the mask

        if (mask == 0)
            return false;

        int bitWidth = fullMask switch
        {
            byte.MaxValue => 8,
            ushort.MaxValue => 16,
            uint.MaxValue => 32,
            _ => 64
        };

        int missingBits = BitOperations.PopCount(mask);
        double density = missingBits / (double)bitWidth;
        return density >= cfg.ValueBitMaskMinDensity;
    }
}