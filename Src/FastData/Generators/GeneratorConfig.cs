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

        LengthData lengthData = props.LengthData;

        MapStrategy lengthStrategy = GetLengthMapStrategy(lengthData.LengthMap);
        if (lengthStrategy == MapStrategy.Equals)
            yield return new LengthEqualEarlyExit(lengthData.LengthMap.Min, lengthData.MinByteCount);
        else if (lengthStrategy == MapStrategy.Range)
            yield return new LengthRangeEarlyExit(lengthData.LengthMap.Min, lengthData.LengthMap.Max, lengthData.MinByteCount, lengthData.MaxByteCount);
        else
            yield return new LengthBitmapEarlyExit(lengthData.LengthMap.Values);

        // if (lengthData.CharDivisor > 1 || lengthData.ByteDivisor > 1)
        // yield return new LengthDivisorEarlyExit(lengthData.CharDivisor, lengthData.ByteDivisor);

        if (ShouldApplyCharMap(props.LengthData.LengthMap.Min, props.CharacterData.AllAscii, enc, IgnoreCase))
        {
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            AsciiMap lastMap = props.CharacterData.LastCharMap;
            MapStrategy firstStrategy = GetCharMapStrategy(firstMap);
            MapStrategy lastStrategy = GetCharMapStrategy(lastMap);

            if (firstStrategy == MapStrategy.Equals)
                yield return new CharEqualsEarlyExit(CharPosition.First, firstMap.Min);
            else if (lastStrategy == MapStrategy.Equals)
                yield return new CharEqualsEarlyExit(CharPosition.Last, lastMap.Min);
            else if (firstStrategy == MapStrategy.Range)
                yield return new CharRangeEarlyExit(CharPosition.First, firstMap.Min, firstMap.Max);
            else if (lastStrategy == MapStrategy.Range)
                yield return new CharRangeEarlyExit(CharPosition.Last, lastMap.Min, lastMap.Max);
            else if (firstStrategy == MapStrategy.Bitmap)
                yield return new CharBitmapEarlyExit(CharPosition.First, firstMap.Low, firstMap.High);
            else if (lastStrategy == MapStrategy.Bitmap)
                yield return new CharBitmapEarlyExit(CharPosition.Last, lastMap.Low, lastMap.High);
        }
        else if (ShouldApplyStringBitMask(props.CharacterData.StringBitMask, props.CharacterData.StringBitMaskBytes, out ulong mask))
            yield return new StringBitMaskEarlyExit(mask, props.CharacterData.StringBitMaskBytes);

        if (props.DeltaData.Prefix.Length != 0 || props.DeltaData.Suffix.Length != 0)
            yield return new StringPrefixSuffixEarlyExit(props.DeltaData.Prefix, props.DeltaData.Suffix);
    }

    private IEnumerable<IEarlyExit> GetEarlyExits(NumericKeyProperties<T> props, uint itemCount, Type structureType)
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
            yield return new ValueRangeEarlyExit<T>(props.MinKeyValue, props.MaxKeyValue); // 1 op: val.Len != len
        else if (IsBitMaskViable(props, out ulong mask))
            yield return new ValueBitMaskEarlyExit(mask); // 2 ops: val & mask != 0
        else
            yield return new ValueRangeEarlyExit<T>(props.MinKeyValue, props.MaxKeyValue); // 3 ops: len < min || len > max
    }

    private MapStrategy GetLengthMapStrategy(LengthBitArray map)
    {
        if (map.BitCount <= 1)
            return MapStrategy.Equals;

        ulong range = (ulong)map.Max - map.Min + 1;
        double density = map.BitCount / (double)range;
        return density >= _cfg.LengthMapMinDensity ? MapStrategy.Range : MapStrategy.Bitmap;
    }

    private bool IsBitMaskViable(NumericKeyProperties<T> props, out ulong mask)
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

    private static bool ShouldApplyCharMap(uint minLength, bool allAscii, GeneratorEncoding encoding, bool ignoreCase)
    {
        if (minLength == 0)
            return false;

        if (!allAscii)
            return false;

        if (ignoreCase && !allAscii)
            return false;

        if (encoding is GeneratorEncoding.ASCII or GeneratorEncoding.UTF8)
            return allAscii;

        return true;
    }

    private MapStrategy GetCharMapStrategy(AsciiMap map)
    {
        if (map.BitCount == 1)
            return MapStrategy.Equals;

        return map.Density >= _cfg.CharMapMinDensity ? MapStrategy.Range : MapStrategy.Bitmap;
    }

    private enum MapStrategy
    {
        Equals,
        Range,
        Bitmap
    }
}