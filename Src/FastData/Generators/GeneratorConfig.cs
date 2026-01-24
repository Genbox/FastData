using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using System.Numerics;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
/// <typeparam name="T">The type of data being generated.</typeparam>
public sealed class GeneratorConfig<T>
{
    private readonly FastDataConfig _cfg;

    private GeneratorConfig(Type structureType, KeyType keyType, HashDetails hashDetails, GeneratorEncoding encoding, FastDataConfig cfg)
    {
        _cfg = cfg;
        StructureType = structureType;
        KeyType = keyType;
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);
        HashDetails = hashDetails;
        Encoding = encoding;
        IgnoreCase = false;
        TrimPrefix = string.Empty;
        TrimSuffix = string.Empty;
    }

    internal GeneratorConfig(Type structureType, KeyType keyType, uint itemCount, NumericKeyProperties<T> props, HashDetails hashDetails, FastDataConfig cfg) : this(structureType, keyType, hashDetails, GeneratorEncoding.Unknown, cfg)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType).ToArray();
        Constants = CreateConstants(props, itemCount);
    }

    internal GeneratorConfig(Type structureType, KeyType keyType, uint itemCount, StringKeyProperties props, HashDetails hashDetails, GeneratorEncoding encoding, string trimPrefix, string trimSuffix, FastDataConfig cfg) : this(structureType, keyType, hashDetails, encoding, cfg)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType, encoding).ToArray();
        Constants = CreateConstants(props, itemCount);
        IgnoreCase = cfg.IgnoreCase;

        // We use an empty string instead of null to simplify calculations later in the pipeline
        TrimPrefix = trimPrefix;
        TrimSuffix = trimSuffix;
    }

    /// <summary>Gets the structure type that the generator will create.</summary>
    public Type StructureType { get; }

    /// <summary>Gets the data type being generated.</summary>
    public KeyType KeyType { get; }

    /// <summary>Gets the encoding used for string keys.</summary>
    public GeneratorEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether string keys should be treated as case-insensitive.</summary>
    public bool IgnoreCase { get; }

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public IEarlyExit[] EarlyExits { get; }

    /// <summary>Gets the constants used by the generator, such as min/max values or string lengths.</summary>
    public Constants<T> Constants { get; }

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; }

    /// <summary>Contains information about the hash function to use.</summary>
    public HashDetails HashDetails { get; }

    public string TrimPrefix { get; }
    public string TrimSuffix { get; }

    public bool TypeReductionEnabled => _cfg.TypeReductionEnabled;

    private static Constants<T> CreateConstants(NumericKeyProperties<T> props, uint itemCount)
    {
        Constants<T> constants = new Constants<T>(itemCount);
        constants.MinValue = props.MinKeyValue;
        constants.MaxValue = props.MaxKeyValue;
        return constants;
    }

    private static Constants<T> CreateConstants(StringKeyProperties props, uint itemCount)
    {
        Constants<T> constants = new Constants<T>(itemCount);
        constants.MinStringLength = props.LengthData.LengthMap.Min;
        constants.MaxStringLength = props.LengthData.LengthMap.Max;
        constants.CharacterClasses = props.CharacterData.CharacterClasses;
        return constants;
    }

    private IEnumerable<IEarlyExit> GetEarlyExits(StringKeyProperties props, uint itemCount, Type structureType, GeneratorEncoding enc)
    {
        //These don't support early exits
        if (structureType == typeof(SingleValueStructure<,>))
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == typeof(ConditionalStructure<,>) && itemCount <= 3)
            yield break;

        //Logic:
        // - If all lengths are the same, we check against that (1 inst)
        // - If lengths are consecutive (5, 6, 7, etc.) we do a range check (2 inst)
        // - If the lengths are non-consecutive (4, 9, 12, etc.) we use a small bitset (4 inst)

        LengthData lengthData = props.LengthData;

        if (ShouldApplyBitSet(lengthData))
            yield return new LengthBitSetEarlyExit(lengthData.LengthMap.Values);
        else
            yield return new MinMaxLengthEarlyExit(lengthData.LengthMap.Min, lengthData.LengthMap.Max, lengthData.MinByteCount, lengthData.MaxByteCount); //Also handles same lengths

        if (ShouldApplyCharRange(props.CharacterData.FirstCharMin, props.CharacterData.LastCharMin, props.LengthData.LengthMap.Min, props.CharacterData.AllAscii, enc, IgnoreCase))
            yield return new CharRangeEarlyExit(props.CharacterData.FirstCharMin, props.CharacterData.FirstCharMax, props.CharacterData.LastCharMin, props.CharacterData.LastCharMax);
        else if (ShouldApplyStringBitMask(props.CharacterData.StringBitMask, props.CharacterData.StringBitMaskBytes, out ulong mask))
            yield return new StringBitMaskEarlyExit(mask, props.CharacterData.StringBitMaskBytes);

        if (props.DeltaData.Prefix.Length != 0 || props.DeltaData.Suffix.Length != 0)
            yield return new PrefixSuffixEarlyExit(props.DeltaData.Prefix, props.DeltaData.Suffix);
    }

    private IEnumerable<IEarlyExit> GetEarlyExits(NumericKeyProperties<T> props, uint itemCount, Type structureType)
    {
        //These don't support early exits
        if (structureType == typeof(SingleValueStructure<,>) || structureType == typeof(BitSetStructure<,>) || structureType == typeof(RangeStructure<,>))
            yield break;

        //There is no point to using early exists if there is just one item
        if (itemCount == 1)
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == typeof(ConditionalStructure<,>) && itemCount <= 3)
            yield break;

        if (ShouldApplyValueBitMask(props, out ulong mask))
            yield return new ValueBitMaskEarlyExit(mask);
        else
            yield return new MinMaxValueEarlyExit<T>(props.MinKeyValue, props.MaxKeyValue);
    }

    private bool ShouldApplyBitSet(LengthData lengthData)
    {
        if (!lengthData.LengthMap.HasBitSet)
            return false;

        if (lengthData.LengthMap.Consecutive)
            return false;

        if (lengthData.LengthMap.Values.Length > _cfg.StringLengthBitSetMaxWords)
            return false;

        uint range = lengthData.LengthMap.Max - lengthData.LengthMap.Min + 1;
        double density = lengthData.LengthMap.BitCount / (double)range;
        return density <= _cfg.StringLengthBitSetMaxDensity;
    }

    private bool ShouldApplyValueBitMask(NumericKeyProperties<T> props, out ulong mask)
    {
        Type keyType = typeof(T);

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
        return density >= _cfg.ValueBitMaskMinDensity;
    }

    private bool ShouldApplyStringBitMask(ulong mask, int byteCount, out ulong stringMask)
    {
        stringMask = mask;

        if (mask == 0 || byteCount <= 0)
            return false;

        int missingBits = BitOperations.PopCount(mask);
        int bitWidth = byteCount * 8;
        double density = missingBits / (double)bitWidth;
        return density >= _cfg.StringBitMaskMinDensity;
    }

    private static bool ShouldApplyCharRange(char firstMin, char lastMin, uint minLength, bool allAscii, GeneratorEncoding encoding, bool ignoreCase)
    {
        if (firstMin == char.MaxValue || lastMin == char.MaxValue)
            return false;

        if (minLength == 0)
            return false;

        if (ignoreCase && !allAscii)
            return false;

        if (encoding is GeneratorEncoding.ASCII or GeneratorEncoding.UTF8)
            return allAscii;

        return true;
    }
}