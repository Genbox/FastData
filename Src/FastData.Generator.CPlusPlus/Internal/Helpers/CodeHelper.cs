using System.Globalization;
using System.Text;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Helpers;

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
        char val => $"'{val}'",
        bool val => val.ToString().ToLowerInvariant(),
        IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}