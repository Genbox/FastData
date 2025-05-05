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

        switch (data)
        {
            case sbyte[] sbyteArr:
                intProps = DataAnalyzer.GetSByteProperties(sbyteArr);
                break;
            case byte[] byteArr:
                uintProps = DataAnalyzer.GetByteProperties(byteArr);
                break;
            case short[] shortArr:
                intProps = DataAnalyzer.GetInt16Properties(shortArr);
                break;
            case ushort[] ushortArr:
                uintProps = DataAnalyzer.GetUInt16Properties(ushortArr);
                break;
            case int[] intArr:
                intProps = DataAnalyzer.GetInt32Properties(intArr);
                break;
            case uint[] uintArr:
                uintProps = DataAnalyzer.GetUInt32Properties(uintArr);
                break;
            case long[] longArr:
                intProps = DataAnalyzer.GetInt64Properties(longArr);
                break;
            case ulong[] ulongArr:
                uintProps = DataAnalyzer.GetUInt64Properties(ulongArr);
                break;
            case string[] stringArr:
                stringProps = DataAnalyzer.GetStringProperties(stringArr);
                break;
            case char[] charArr:
                charProps = DataAnalyzer.GetCharProperties(charArr);
                break;
            case float[] floatArr:
                floatProps = DataAnalyzer.GetSingleProperties(floatArr);
                break;
            case double[] doubleArr:
                floatProps = DataAnalyzer.GetDoubleProperties(doubleArr);
                break;
            case DataType.Boolean:
            case DataType.Unknown:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException($"Unknown data type: {typeof(T)}");
        }

        return new DataProperties((uint)data.Length, (DataType)Enum.Parse(typeof(DataType), typeof(T).Name), stringProps, intProps, uintProps, charProps, floatProps);
    }

    public StringProperties? StringProps { get; }
    public IntegerProperties? IntProps { get; }
    public UnsignedIntegerProperties? UIntProps { get; }
    public CharProperties? CharProps { get; }
    public FloatProperties? FloatProps { get; }
    public DataType DataType { get; }
    public uint ItemCount { get; }
}