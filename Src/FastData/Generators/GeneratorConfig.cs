using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
/// <typeparam name="T">The type of data being generated.</typeparam>
public sealed class GeneratorConfig<T>
{
    private GeneratorConfig(StructureType structureType, DataType dataType, HashDetails hashDetails, GeneratorFlags flags)
    {
        StructureType = structureType;
        DataType = dataType;
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.Now);
        HashDetails = hashDetails;
        Flags = flags;
    }

    internal GeneratorConfig(StructureType structureType, DataType dataType, uint itemCount, ValueProperties<T> props, HashDetails hashDetails, GeneratorFlags flags) : this(structureType, dataType, hashDetails, flags)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType).ToArray();
        Constants = CreateConstants(props, itemCount);
    }

    internal GeneratorConfig(StructureType structureType, DataType dataType, uint itemCount, StringProperties props, StringComparison stringComparison, HashDetails hashDetails, GeneratorEncoding encoding, GeneratorFlags flags) : this(structureType, dataType, hashDetails, flags)
    {
        EarlyExits = GetEarlyExits(props, itemCount, structureType, encoding).ToArray();
        Constants = CreateConstants(props, itemCount);
        StringComparison = stringComparison;
    }

    /// <summary>Gets the structure type that the generator will create.</summary>
    public StructureType StructureType { get; }

    /// <summary>Gets the string comparison mode to use.</summary>
    public StringComparison StringComparison { get; }

    /// <summary>Gets the data type being generated.</summary>
    public DataType DataType { get; }

    /// <summary>Gets the set of early exit strategies used by the generator to optimize code generation.</summary>
    public IEarlyExit[] EarlyExits { get; }

    /// <summary>Gets the constants used by the generator, such as min/max values or string lengths.</summary>
    public Constants<T> Constants { get; }

    /// <summary>Gets the metadata about the generator, such as version and creation time.</summary>
    public Metadata Metadata { get; }

    /// <summary>Contains information about the hash function to use.</summary>
    public HashDetails HashDetails { get; }
    public GeneratorFlags Flags { get; }

    private static Constants<T> CreateConstants(ValueProperties<T> props, uint itemCount)
    {
        Constants<T> constants = new Constants<T>(itemCount);
        constants.MinValue = props.MinValue;
        constants.MaxValue = props.MaxValue;
        return constants;
    }

    private static Constants<T> CreateConstants(StringProperties props, uint itemCount)
    {
        Constants<T> constants = new Constants<T>(itemCount);
        constants.MinStringLength = props.LengthData.Min;
        constants.MaxStringLength = props.LengthData.Max;
        return constants;
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(StringProperties props, uint itemCount, StructureType structureType, GeneratorEncoding enc)
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

        if (props.LengthData.Max <= 64 && !props.LengthData.LengthMap.Consecutive)
            yield return new LengthBitSetEarlyExit(props.LengthData.LengthMap.FirstValue);
        else
        {
            uint minByteCount = enc == GeneratorEncoding.UTF8 ? props.LengthData.MinUtf8ByteCount : props.LengthData.MinUtf16ByteCount;
            uint maxByteCount = enc == GeneratorEncoding.UTF8 ? props.LengthData.MaxUtf8ByteCount : props.LengthData.MaxUtf16ByteCount;

            yield return new MinMaxLengthEarlyExit(props.LengthData.Min, props.LengthData.Max, minByteCount, maxByteCount); //Also handles same lengths
        }
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(ValueProperties<T> props, uint itemCount, StructureType structureType)
    {
        //There is no point to using early exists if there is just one item
        if (itemCount == 1)
            yield break;

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == StructureType.Conditional && itemCount <= 3)
            yield break;

        yield return new MinMaxValueEarlyExit<T>(props.MinValue, props.MaxValue);
    }
}