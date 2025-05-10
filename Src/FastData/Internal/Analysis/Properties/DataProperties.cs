using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal class DataProperties<T>
{
    private DataProperties(uint items,
                           DataType type,
                           StringProperties? stringProps,
                           IntegerProperties<T>? intProps,
                           UnsignedIntegerProperties<T>? uintProps,
                           CharProperties<T>? charProps,
                           FloatProperties<T>? floatProps)
    {
        ItemCount = items;
        DataType = type;

        StringProps = stringProps;
        IntProps = intProps;
        UIntProps = uintProps;
        CharProps = charProps;
        FloatProps = floatProps;
    }

    internal static DataProperties<T> Create(T[] data)
    {
        StringProperties? stringProps = null;
        IntegerProperties<T>? intProps = null;
        UnsignedIntegerProperties<T>? uintProps = null;
        CharProperties<T>? charProps = null;
        FloatProperties<T>? floatProps = null;

        DataType dataType = (DataType)Enum.Parse(typeof(DataType), typeof(T).Name);

        //The 'when' in the switch seems redundant, but it isn't. C# apparently interprets byte[] as sbyte[] automatically

        switch (data)
        {
            case sbyte[] sbyteArr when dataType == DataType.SByte:
                intProps = DataAnalyzer.GetSByteProperties<T>(sbyteArr);
                break;
            case byte[] byteArr when dataType == DataType.Byte:
                uintProps = DataAnalyzer.GetByteProperties<T>(byteArr);
                break;
            case short[] shortArr when dataType == DataType.Int16:
                intProps = DataAnalyzer.GetInt16Properties<T>(shortArr);
                break;
            case ushort[] ushortArr when dataType == DataType.UInt16:
                uintProps = DataAnalyzer.GetUInt16Properties<T>(ushortArr);
                break;
            case int[] intArr when dataType == DataType.Int32:
                intProps = DataAnalyzer.GetInt32Properties<T>(intArr);
                break;
            case uint[] uintArr when dataType == DataType.UInt32:
                uintProps = DataAnalyzer.GetUInt32Properties<T>(uintArr);
                break;
            case long[] longArr when dataType == DataType.Int64:
                intProps = DataAnalyzer.GetInt64Properties<T>(longArr);
                break;
            case ulong[] ulongArr when dataType == DataType.UInt64:
                uintProps = DataAnalyzer.GetUInt64Properties<T>(ulongArr);
                break;
            case string[] stringArr when dataType == DataType.String:
                stringProps = DataAnalyzer.GetStringProperties(stringArr);
                break;
            case char[] charArr when dataType == DataType.Char:
                charProps = DataAnalyzer.GetCharProperties<T>(charArr);
                break;
            case float[] floatArr when dataType == DataType.Single:
                floatProps = DataAnalyzer.GetSingleProperties<T>(floatArr);
                break;
            case double[] doubleArr when dataType == DataType.Double:
                floatProps = DataAnalyzer.GetDoubleProperties<T>(doubleArr);
                break;
            case bool[]:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException($"Unknown data type: {typeof(T)}");
        }

        return new DataProperties<T>((uint)data.Length, dataType, stringProps, intProps, uintProps, charProps, floatProps);
    }

    public StringProperties? StringProps { get; }
    public IntegerProperties<T>? IntProps { get; }
    public UnsignedIntegerProperties<T>? UIntProps { get; }
    public CharProperties<T>? CharProps { get; }
    public FloatProperties<T>? FloatProps { get; }
    public DataType DataType { get; }
    public uint ItemCount { get; }
}