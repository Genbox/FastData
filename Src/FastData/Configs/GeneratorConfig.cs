using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExits;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Configs;

public sealed class GeneratorConfig<T>
{
    internal GeneratorConfig(StructureType structureType, StringComparison stringComparison, DataProperties<T> props, IStringHash? stringHash)
    {
        StructureType = structureType;
        StringComparison = stringComparison;
        StringHash = stringHash;
        DataType = props.DataType;
        EarlyExits = GetEarlyExits(props, structureType);
        Constants = CreateConstants(props);
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.Now);
    }

    public StructureType StructureType { get; }
    public StringComparison StringComparison { get; }
    public DataType DataType { get; }
    public IEarlyExit[] EarlyExits { get; }
    public IStringHash? StringHash { get; set; }
    public Constants<T> Constants { get; }
    public Metadata Metadata { get; }

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
            yield return new LengthBitSetEarlyExit(prop.LengthData.LengthMap.BitSet);
        else
            yield return new MinMaxLengthEarlyExit(prop.LengthData.Min, prop.LengthData.Max); //Also handles same lengths
    }

    private static IEnumerable<IEarlyExit> GetEarlyExits(IHasMinMax<T> prop)
    {
        yield return new MinMaxValueEarlyExit<T>(prop.MinValue, prop.MaxValue);
    }
}