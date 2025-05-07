using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal class DataProperties
{
    private DataProperties(uint items,
                           DataType type,
                           StringProperties? stringProps,
                           IntegerProperties? intProps,
                           UnsignedIntegerProperties? uintProps,
                           CharProperties? charProps,
                           FloatProperties? floatProps)
    {
        ItemCount = items;
        DataType = type;

        StringProps = stringProps;
        IntProps = intProps;
        UIntProps = uintProps;
        CharProps = charProps;
        FloatProps = floatProps;
    }

    internal static DataProperties Create<T>(T[] data)
    {
        StringProperties? stringProps = null;
        IntegerProperties? intProps = null;
        UnsignedIntegerProperties? uintProps = null;
        CharProperties? charProps = null;
        FloatProperties? floatProps = null;

        DataType dataType = (DataType)Enum.Parse(typeof(DataType), typeof(T).Name);

        //The 'when' in the switch seems redundant, but it isn't. C# apparently interprets byte[] as sbyte[] automatically

        switch (data)
        {
            case sbyte[] sbyteArr when dataType == DataType.SByte:
                intProps = DataAnalyzer.GetSByteProperties(sbyteArr);
                break;
            case byte[] byteArr when dataType == DataType.Byte:
                uintProps = DataAnalyzer.GetByteProperties(byteArr);
                break;
            case short[] shortArr when dataType == DataType.Int16:
                intProps = DataAnalyzer.GetInt16Properties(shortArr);
                break;
            case ushort[] ushortArr when dataType == DataType.UInt16:
                uintProps = DataAnalyzer.GetUInt16Properties(ushortArr);
                break;
            case int[] intArr when dataType == DataType.Int32:
                intProps = DataAnalyzer.GetInt32Properties(intArr);
                break;
            case uint[] uintArr when dataType == DataType.UInt32:
                uintProps = DataAnalyzer.GetUInt32Properties(uintArr);
                break;
            case long[] longArr when dataType == DataType.Int64:
                intProps = DataAnalyzer.GetInt64Properties(longArr);
                break;
            case ulong[] ulongArr when dataType == DataType.UInt64:
                uintProps = DataAnalyzer.GetUInt64Properties(ulongArr);
                break;
            case string[] stringArr when dataType == DataType.String:
                stringProps = DataAnalyzer.GetStringProperties(stringArr);
                break;
            case char[] charArr when dataType == DataType.Char:
                charProps = DataAnalyzer.GetCharProperties(charArr);
                break;
            case float[] floatArr when dataType == DataType.Single:
                floatProps = DataAnalyzer.GetSingleProperties(floatArr);
                break;
            case double[] doubleArr when dataType == DataType.Double:
                floatProps = DataAnalyzer.GetDoubleProperties(doubleArr);
                break;
            case bool[]:
            case DataType.Unknown:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException($"Unknown data type: {typeof(T)}");
        }

        return new DataProperties((uint)data.Length, dataType, stringProps, intProps, uintProps, charProps, floatProps);
    }

    public StringProperties? StringProps { get; }
    public IntegerProperties? IntProps { get; }
    public UnsignedIntegerProperties? UIntProps { get; }
    public CharProperties? CharProps { get; }
    public FloatProperties? FloatProps { get; }
    public DataType DataType { get; }
    public uint ItemCount { get; }
}