using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Generator.Rust.Internal.Helpers;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
internal static class CodeHelper
{
    internal static string GetSmallestUnsignedType(long value) => GetSmallestUnsignedType((ulong)value);

    internal static string GetSmallestUnsignedType(ulong value) => value switch
    {
        <= byte.MaxValue => "u8",
        <= ushort.MaxValue => "u16",
        <= uint.MaxValue => "u32",
        _ => "u64"
    };

    internal static string GetSmallestSignedType(long value) => value switch
    {
        <= sbyte.MaxValue => "i8",
        <= short.MaxValue => "i16",
        <= int.MaxValue => "i32",
        _ => "i64"
    };

    internal static string ToValueLabel(object? value) => value switch
    {
        null => "\"\"",
        string val => $"\"{val}\"",
        char val => $"'{val}'",
        float val => val switch
        {
            float.MaxValue => "f32::MAX",
            float.MinValue => "f32::MIN",
            _ => val.ToString("0.0", CultureInfo.InvariantCulture)
        },
        double val => val switch
        {
            double.MaxValue => "f64::MAX",
            double.MinValue => "f64::MIN",
            _ => val.ToString("0.0", CultureInfo.InvariantCulture)
        },
        bool val => val.ToString().ToLowerInvariant(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    internal static string ToValueLabel(object? value, DataType dataType) => dataType switch
    {
        DataType.String => $"\"{value}\"",
        DataType.Char => $"'{value}'",
        DataType.Single => (double)value == float.MaxValue ? "f32::MAX" : (double)value == float.MinValue ? "f32::MIN" : ((double)value).ToString("0.0", CultureInfo.InvariantCulture),
        DataType.Double => (double)value == double.MaxValue ? "f64::MAX" : (double)value == double.MinValue ? "f64::MIN" : ((double)value).ToString("0.0", CultureInfo.InvariantCulture),
        DataType.Boolean => ((bool)value).ToString().ToLowerInvariant(),
        _ => value.ToString()!
    };
}