using System.Runtime.InteropServices;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DataProperties
{
    public DataProperties(object[] data)
    {
        ItemCount = data.Length;
        DataType = (DataType)Enum.Parse(typeof(DataType), data[0].GetType().Name);

        switch (DataType)
        {
            case DataType.SByte:
                IntProps = DataAnalyzer.GetSByteProperties(data);
                break;
            case DataType.Byte:
                UIntProps = DataAnalyzer.GetByteProperties(data);
                break;
            case DataType.Int16:
                IntProps = DataAnalyzer.GetInt16Properties(data);
                break;
            case DataType.UInt16:
                UIntProps = DataAnalyzer.GetUInt16Properties(data);
                break;
            case DataType.Int32:
                IntProps = DataAnalyzer.GetInt32Properties(data);
                break;
            case DataType.UInt32:
                UIntProps = DataAnalyzer.GetUInt32Properties(data);
                break;
            case DataType.Int64:
                IntProps = DataAnalyzer.GetInt64Properties(data);
                break;
            case DataType.UInt64:
                UIntProps = DataAnalyzer.GetUInt64Properties(data);
                break;
            case DataType.String:
                StringProps = DataAnalyzer.GetStringProperties(data);
                break;
            case DataType.Char:
                CharProps = DataAnalyzer.GetCharProperties(data);
                break;
            case DataType.Single:
                FloatProps = DataAnalyzer.GetSingleProperties(data);
                break;
            case DataType.Double:
                FloatProps = DataAnalyzer.GetDoubleProperties(data);
                break;
            case DataType.Boolean:
            case DataType.Unknown:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException($"Unknown data type: {DataType}");
        }
    }

    public StringProperties? StringProps { get; }
    public IntegerProperties? IntProps { get; }
    public UnsignedIntegerProperties? UIntProps { get; }
    public CharProperties? CharProps { get; }
    public FloatProperties? FloatProps { get; }
    public DataType DataType { get; }
    public int ItemCount { get; }
}