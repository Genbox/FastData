using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using System.Numerics;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
/// <typeparam name="T">The type of data being generated.</typeparam>
public sealed class GeneratorConfig<T>
{
    private const int MaxLengthBitSetWords = 4;
    private const double MaxLengthBitSetDensity = 0.5;
    private const double MinValueBitMaskDensity = 0.25;

    private GeneratorConfig(StructureType structureType, KeyType keyType, HashDetails hashDetails, GeneratorFlags flags)
    {
        StructureType = structureType;
        KeyType = keyType;
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.UtcNow);
        HashDetails = hashDetails;
        Flags = flags;
        IgnoreCase = false;
        TrimPrefix = string.Empty;
        TrimSuffix = string.Empty;
    }

    internal GeneratorConfig(StructureType structureType, KeyType keyType, uint itemCount, NumericKeyProperties<T> props, HashDetails hashDetails, GeneratorFlags flags) : this(structureType, keyType, hashDetails, flags)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType).ToArray();
        Constants = CreateConstants(props, itemCount);
    }

    internal GeneratorConfig(StructureType structureType, KeyType keyType, uint itemCount, StringKeyProperties props, bool ignoreCase, HashDetails hashDetails, GeneratorEncoding encoding, GeneratorFlags flags, string? trimPrefix, string? trimSuffix) : this(structureType, keyType, hashDetails, flags)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType, encoding).ToArray();
        Constants = CreateConstants(props, itemCount);
        IgnoreCase = ignoreCase;

        // We use an empty string instead of null to simplify calculations later in the pipeline
        TrimPrefix = trimPrefix ?? string.Empty;
        TrimSuffix = trimSuffix ?? string.Empty;
    }

    /// <summary>Gets the structure type that the generator will create.</summary>
    public StructureType StructureType { get; }

    /// <summary>Gets the data type being generated.</summary>
    public KeyType KeyType { get; }

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

    public GeneratorFlags Flags { get; }

    public string TrimPrefix { get; }
    public string TrimSuffix { get; }

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
        return constants;
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(StringKeyProperties props, uint itemCount, StructureType structureType, GeneratorEncoding enc)
    {
        //There is no point to using early exists if there is just one item
        if (itemCount == 1)
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == StructureType.Conditional && itemCount <= 3)
            yield break;

        //Logic:
        // - If all lengths are the same, we check against that (1 inst)
        // - If lengths are consecutive (5, 6, 7, etc.) we do a range check (2 inst)
        // - If the lengths are non-consecutive (4, 9, 12, etc.) we use a small bitset (4 inst)

        LengthData lengthData = props.LengthData;

        if (ShouldUseBitSet(lengthData))
            yield return new LengthBitSetEarlyExit(lengthData.LengthMap.Values);
        else
        {
            uint minByteCount = enc == GeneratorEncoding.UTF8 ? lengthData.MinUtf8ByteCount : lengthData.MinUtf16ByteCount;
            uint maxByteCount = enc == GeneratorEncoding.UTF8 ? lengthData.MaxUtf8ByteCount : lengthData.MaxUtf16ByteCount;

            yield return new MinMaxLengthEarlyExit(lengthData.LengthMap.Min, lengthData.LengthMap.Max, minByteCount, maxByteCount); //Also handles same lengths
        }
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(NumericKeyProperties<T> props, uint itemCount, StructureType structureType)
    {
        //There is no point to using early exists if there is just one item
        if (itemCount == 1)
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == StructureType.Conditional && itemCount <= 3)
            yield break;

        if (TryGetValueBitMask(props, out ulong mask))
            yield return new ValueBitMaskEarlyExit(mask);
        else
            yield return new MinMaxValueEarlyExit<T>(props.MinKeyValue, props.MaxKeyValue);
    }

    private static bool ShouldUseBitSet(LengthData lengthData)
    {
        if (!lengthData.LengthMap.HasBitSet)
            return false;

        if (lengthData.LengthMap.Consecutive)
            return false;

        if (lengthData.LengthMap.Values.Length > MaxLengthBitSetWords)
            return false;

        uint range = lengthData.LengthMap.Max - lengthData.LengthMap.Min + 1;
        double density = lengthData.LengthMap.BitCount / (double)range;
        return density <= MaxLengthBitSetDensity;
    }

    private static bool TryGetValueBitMask(NumericKeyProperties<T> props, out ulong mask)
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
        return density >= MinValueBitMaskDensity;
    }
}