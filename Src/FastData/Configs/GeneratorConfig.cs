using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Optimization;

namespace Genbox.FastData.Configs;

public class GeneratorConfig
{
    internal GeneratorConfig(StructureType structureType, StringComparison stringComparison, DataProperties props, IHashSpec hashSpec)
    {
        StructureType = structureType;
        StringComparison = stringComparison;
        HashSpec = hashSpec;
        DataType = props.DataType;
        EarlyExits = GetEarlyExits(props);
        Constants = CreateConstants(props);
        Metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.Now);
    }

    public StructureType StructureType { get; }
    public StringComparison StringComparison { get; }
    public DataType DataType { get; }
    public IEarlyExit[] EarlyExits { get; }
    public IHashSpec HashSpec { get; set; }
    public Constants Constants { get; }
    public Metadata Metadata { get; }

    private static Constants CreateConstants(DataProperties props)
    {
        Constants constants = new Constants((uint)props.ItemCount);

        if (props.StringProps.HasValue)
        {
            constants.MinValue = props.StringProps.Value.LengthData.Min;
            constants.MaxValue = props.StringProps.Value.LengthData.Max;
        }
        else if (props.IntProps.HasValue)
        {
            constants.MinValue = props.IntProps.Value.MinValue;
            constants.MaxValue = props.IntProps.Value.MaxValue;
        }
        else if (props.UIntProps.HasValue)
        {
            constants.MinValue = props.UIntProps.Value.MinValue;
            constants.MaxValue = props.UIntProps.Value.MaxValue;
        }
        else if (props.FloatProps.HasValue)
        {
            constants.MinValue = props.FloatProps.Value.MinValue;
            constants.MaxValue = props.FloatProps.Value.MaxValue;
        }
        else if (props.CharProps.HasValue)
        {
            constants.MinValue = props.CharProps.Value.MinValue;
            constants.MaxValue = props.CharProps.Value.MaxValue;
        }
        return constants;
    }

    private static IEarlyExit[] GetEarlyExits(DataProperties props)
    {
        if (props.StringProps.HasValue)
            return Optimizer.GetEarlyExits(props.StringProps.Value).ToArray();
        if (props.IntProps.HasValue)
            return Optimizer.GetEarlyExits(props.IntProps.Value).ToArray();
        if (props.UIntProps.HasValue)
            return Optimizer.GetEarlyExits(props.UIntProps.Value).ToArray();
        if (props.CharProps.HasValue)
            return Optimizer.GetEarlyExits(props.CharProps.Value).ToArray();
        if (props.FloatProps.HasValue)
            return Optimizer.GetEarlyExits(props.FloatProps.Value).ToArray();
        return [];
    }
}