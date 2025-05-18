using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Optimization;

namespace Genbox.FastData.Configs;

public class GeneratorConfig<T>
{
    internal GeneratorConfig(StructureType structureType, StringComparison stringComparison, DataProperties<T> props, IStringHash stringHash)
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
    public IStringHash StringHash { get; set; }
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
            return Optimizer.GetEarlyExits(props.StringProps).ToArray();

        //Conditional structures are not very useful with less than 3 items as checks costs more than the benefits
        if (structureType == StructureType.Conditional && props.ItemCount <= 3)
            return [];

        if (props.IntProps != null)
            return Optimizer.GetEarlyExits(props.IntProps).ToArray();

        if (props.FloatProps != null)
            return Optimizer.GetEarlyExits(props.FloatProps).ToArray();

        return [];
    }
}