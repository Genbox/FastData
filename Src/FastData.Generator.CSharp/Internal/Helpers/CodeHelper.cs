using System.Globalization;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Helpers;

internal static class CodeHelper
{
    internal static string GetSmallestUnsignedType(long value) => GetSmallestUnsignedType((ulong)value);

    internal static string GetSmallestUnsignedType(ulong value) => value switch
    {
        <= byte.MaxValue => "byte",
        <= ushort.MaxValue => "ushort",
        <= uint.MaxValue => "uint",
        _ => "ulong"
    };

    internal static string GetSmallestSignedType(long value) => value switch
    {
        <= sbyte.MaxValue => "sbyte",
        <= short.MaxValue => "short",
        <= int.MaxValue => "int",
        _ => "long"
    };

    internal static string? ToValueLabel(object? value) => value switch
    {
        null => "null",
        string val => $"\"{val}\"",
        char val => $"'{val}'",
        bool val => val.ToString().ToLowerInvariant(),
        float val => val + "f",
        uint val => val + "u",
        IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    internal static string ToValueLabel(object? value, DataType dataType) => dataType switch
    {
        DataType.String => $"\"{value}\"",
        DataType.Char => $"'{value}'",
        DataType.UInt64 => value + "ul",
        DataType.Int64 => value + "l",
        DataType.Single => value + "f",
        DataType.Boolean => value.ToString().ToLowerInvariant(),
        _ => value.ToString()
    };
}