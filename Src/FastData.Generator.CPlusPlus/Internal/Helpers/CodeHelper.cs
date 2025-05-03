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

    internal static string ToValueLabel(object? value) => value switch
    {
        null => "\"\"",
        string val => $"\"{val}\"",
        char val => ((byte)val).ToString(CultureInfo.InvariantCulture),
        ulong val => val + "ull",
        long val => val + "ll",
        uint val => val + "u",
        float val => val switch
        {
            float.MaxValue => "std::numeric_limits<float>::max()",
            float.MinValue => "std::numeric_limits<float>::lowest()",
            _ => val.ToString("0.0", CultureInfo.InvariantCulture) + "f"
        },
        double val => val switch
        {
            double.MaxValue => "std::numeric_limits<double>::max()",
            double.MinValue => "std::numeric_limits<double>::lowest()",
            _ => val.ToString("0.0", CultureInfo.InvariantCulture)
        },
        bool val => val.ToString().ToLowerInvariant(),
        IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    internal static string ToValueLabel(object? value, DataType dataType) => dataType switch
    {
        DataType.String => $"\"{value}\"",
        DataType.Char => $"{(byte)(char)value}",
        DataType.UInt64 => value + "ull",
        DataType.Int64 => value + "ll",
        DataType.UInt32 => value + "u",
        DataType.Single => (double)value == float.MaxValue ? "std::numeric_limits<float>::max()" : (double)value == float.MinValue ? "std::numeric_limits<float>::lowest()" : ((double)value).ToString("0.0", CultureInfo.InvariantCulture) + "f",
        DataType.Double => (double)value == double.MaxValue ? "std::numeric_limits<double>::max()" : (double)value == double.MinValue ? "std::numeric_limits<double>::lowest()" : ((double)value).ToString("0.0", CultureInfo.InvariantCulture),
        DataType.Boolean => value.ToString().ToLowerInvariant(),
        _ => value.ToString()
    };
}