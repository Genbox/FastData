using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Helpers;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
internal static class CodeHelper
{
    internal static string GetSmallestUnsignedType(long value) => GetSmallestUnsignedType((ulong)value);

    internal static string GetSmallestUnsignedType(ulong value) => value switch
    {
        <= byte.MaxValue => "uint8_t",
        <= ushort.MaxValue => "uint16_t",
        <= uint.MaxValue => "uint32_t",
        _ => "uint64_t"
    };

    internal static string GetSmallestSignedType(long value) => value switch
    {
        <= sbyte.MaxValue => "int8_t",
        <= short.MaxValue => "int16_t",
        <= int.MaxValue => "int32_t",
        _ => "int64_t"
    };

    internal static string ToValueLabel<T>(T? value) => value switch
    {
        null => "\"\"",
        string val => $"\"{val}\"",
        char val => ((byte)val).ToString(CultureInfo.InvariantCulture),
        int val => val switch
        {
            int.MaxValue => "std::numeric_limits<int32_t>::max()",
            int.MinValue => "std::numeric_limits<int32_t>::lowest()",
            _ => val + "ll"
        },
        uint val => val + "u",
        long val => val switch
        {
            long.MaxValue => "std::numeric_limits<int64_t>::max()",
            long.MinValue => "std::numeric_limits<int64_t>::lowest()",
            _ => val + "ll"
        },
        ulong val => val + "ll",
        float val => val switch
        {
            float.MaxValue => "std::numeric_limits<float>::max()",
            float.MinValue => "std::numeric_limits<float>::lowest()",
            _ => val.ToString("0.0", NumberFormatInfo.InvariantInfo) + "f"
        },
        double val => val switch
        {
            double.MaxValue => "std::numeric_limits<double>::max()",
            double.MinValue => "std::numeric_limits<double>::lowest()",
            _ => val.ToString("0.0", NumberFormatInfo.InvariantInfo)
        },
        bool val => val.ToString().ToLowerInvariant(),
        IFormattable val => val.ToString(null, NumberFormatInfo.InvariantInfo),
        _ => value.ToString()!
    };

    internal static string ToValueLabel(object? value, DataType dataType) => dataType switch
    {
        DataType.String => $"\"{value}\"",
        DataType.Char => $"{(byte)(char)value}",
        DataType.Int32 => (long)value == int.MaxValue ? "std::numeric_limits<int32_t>::max()" : (long)value == int.MinValue ? "std::numeric_limits<int32_t>::lowest()" : value.ToString(),
        DataType.UInt32 => value + "u",
        DataType.Int64 => (long)value == long.MaxValue ? "std::numeric_limits<int64_t>::max()" : (long)value == long.MinValue ? "std::numeric_limits<int64_t>::lowest()" : value + "ll",
        DataType.UInt64 => value + "ull",
        DataType.Single => (double)value == float.MaxValue ? "std::numeric_limits<float>::max()" : (double)value == float.MinValue ? "std::numeric_limits<float>::lowest()" : ((double)value).ToString("0.0", NumberFormatInfo.InvariantInfo) + "f",
        DataType.Double => (double)value == double.MaxValue ? "std::numeric_limits<double>::max()" : (double)value == double.MinValue ? "std::numeric_limits<double>::lowest()" : ((double)value).ToString("0.0", NumberFormatInfo.InvariantInfo),
        DataType.Boolean => value.ToString().ToLowerInvariant(),
        _ => value.ToString()
    };
}