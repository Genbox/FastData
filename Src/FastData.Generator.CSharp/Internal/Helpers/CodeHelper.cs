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

    internal static string ToValueLabel<T>(T? value) => value switch
    {
        null => "null",
        string val => $"\"{val}\"",
        char val => $"'{val}'",
        ulong val => val + "ul",
        long val => val + "l",
        uint val => val + "u",
        float val => val + "f",
        bool val => val.ToString().ToLowerInvariant(),
        IFormattable val => val.ToString(null, NumberFormatInfo.InvariantInfo),
        _ => value.ToString()!
    };

    internal static string ToValueLabel(object? value, DataType dataType) => dataType switch
    {
        DataType.String => $"\"{value}\"",
        DataType.Char => $"'{value}'",
        DataType.UInt64 => value + "ul",
        DataType.Int64 => value + "l",
        DataType.UInt32 => value + "u",
        DataType.Single => value + "f",
        DataType.Boolean => value.ToString().ToLowerInvariant(),
        _ => value.ToString()
    };
}