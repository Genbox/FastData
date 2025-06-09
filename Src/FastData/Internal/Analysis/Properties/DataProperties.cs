using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed class DataProperties<T> where T : notnull
{
    private DataProperties(uint items, DataType type, StringProperties? stringProps, IntegerProperties<T>? intProps, FloatProperties<T>? floatProps)
    {
        ItemCount = items;
        DataType = type;

        StringProps = stringProps;
        IntProps = intProps;
        FloatProps = floatProps;
    }

    internal StringProperties? StringProps { get; }
    internal IntegerProperties<T>? IntProps { get; }
    internal FloatProperties<T>? FloatProps { get; }
    internal DataType DataType { get; }
    internal uint ItemCount { get; }

    internal static DataProperties<T> Create(ReadOnlySpan<T> data)
    {
        DataType dataType = (DataType)Enum.Parse(typeof(DataType), typeof(T).Name);

        //The 'when' in the switch seems redundant, but it isn't. C# apparently interprets byte[] as sbyte[] automatically
        IntegerProperties<T>? intProps = dataType switch
        {
            DataType.Char => DataAnalyzer.GetCharProperties<T>(UnsafeHelper.ConvertSpan<T, char>(data)),
            DataType.SByte => DataAnalyzer.GetSByteProperties<T>(UnsafeHelper.ConvertSpan<T, sbyte>(data)),
            DataType.Byte => DataAnalyzer.GetByteProperties<T>(UnsafeHelper.ConvertSpan<T, byte>(data)),
            DataType.Int16 => DataAnalyzer.GetInt16Properties<T>(UnsafeHelper.ConvertSpan<T, short>(data)),
            DataType.UInt16 => DataAnalyzer.GetUInt16Properties<T>(UnsafeHelper.ConvertSpan<T, ushort>(data)),
            DataType.Int32 => DataAnalyzer.GetInt32Properties<T>(UnsafeHelper.ConvertSpan<T, int>(data)),
            DataType.UInt32 => DataAnalyzer.GetUInt32Properties<T>(UnsafeHelper.ConvertSpan<T, uint>(data)),
            DataType.Int64 => DataAnalyzer.GetInt64Properties<T>(UnsafeHelper.ConvertSpan<T, long>(data)),
            DataType.UInt64 => DataAnalyzer.GetUInt64Properties<T>(UnsafeHelper.ConvertSpan<T, ulong>(data)),
            _ => null
        };

        FloatProperties<T>? floatProps = dataType switch
        {
            DataType.Single => DataAnalyzer.GetSingleProperties<T>(UnsafeHelper.ConvertSpan<T, float>(data)),
            DataType.Double => DataAnalyzer.GetDoubleProperties<T>(UnsafeHelper.ConvertSpan<T, double>(data)),
            _ => null
        };

        StringProperties? stringProps = dataType == DataType.String ? DataAnalyzer.GetStringProperties(data) : null;

        return new DataProperties<T>((uint)data.Length, dataType, stringProps, intProps, floatProps);
    }
}