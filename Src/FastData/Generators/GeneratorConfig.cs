using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Generators;

/// <summary>Provides configuration data for code generators in the FastData library.</summary>
/// <typeparam name="T">The type of data being generated.</typeparam>
public sealed class GeneratorConfig<T> where T : notnull
{
    internal GeneratorConfig(StructureType structureType, StringComparison stringComparison, DataProperties<T> props)
    {
        StructureType = structureType;
        StringComparison = stringComparison;
        DataType = props.DataType;
        EarlyExits = GetEarlyExits(props, structureType);
        Constants = CreateConstants(props);
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.Now);
        HashInfo = new HashInfo(props.FloatProps?.hasZeroOrNaN ?? false);
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
    public HashInfo HashInfo { get; }

    private static Constants<T> CreateConstants(DataProperties<T> props)
    {
        Constants<T> constants = new Constants<T>(props.ItemCount);

        if (props.StringProps != null)
        {
            constants.MinStringLength = props.StringProps.LengthData.Min;
            constants.MaxStringLength = props.StringProps.LengthData.Max;
        }
        else if (props.IntProps != null)
        {
            constants.MinValue = props.IntProps.MinValue;
            constants.MaxValue = props.IntProps.MaxValue;
        }
        else if (props.FloatProps != null)
        {
            constants.MinValue = props.FloatProps.MinValue;
            constants.MaxValue = props.FloatProps.MaxValue;
        }

        return constants;
    }

    private static IEarlyExit[] GetEarlyExits(DataProperties<T> props, StructureType structureType)
    {
        //There is no point to using early exists if there is just one item
        if (props.ItemCount == 1)
            return [];

        if (props.StringProps != null)
            return GetEarlyExits(props.StringProps).ToArray();

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == StructureType.Conditional && props.ItemCount <= 3)
            return [];

        if (props.IntProps != null)
            return GetEarlyExits(props.IntProps).ToArray();

        if (props.FloatProps != null)
            return GetEarlyExits(props.FloatProps).ToArray();

        return [];
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(StringProperties prop)
    {
        //Logic:
        // - If all lengths are the same, we check against that (1 inst)
        // - If lengths are consecutive (5, 6, 7, etc.) we do a range check (2 inst)
        // - If the lengths are non-consecutive (4, 9, 12, etc.) we use a small bitset (4 inst)

        if (prop.LengthData.Max <= 64 && !prop.LengthData.LengthMap.Consecutive)
            yield return new LengthBitSetEarlyExit(prop.LengthData.LengthMap.FirstValue);
        else
            yield return new MinMaxLengthEarlyExit(prop.LengthData.Min, prop.LengthData.Max); //Also handles same lengths
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(IHasMinMax<T> prop)
    {
        yield return new MinMaxValueEarlyExit<T>(prop.MinValue, prop.MaxValue);
    }
}