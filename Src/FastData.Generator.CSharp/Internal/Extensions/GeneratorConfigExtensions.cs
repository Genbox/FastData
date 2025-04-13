using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config) => config.DataType switch
    {
        DataType.String => "string",
        DataType.Boolean => "bool",
        DataType.SByte => "sbyte",
        DataType.Byte => "byte",
        DataType.Char => "char",
        DataType.Int16 => "short",
        DataType.UInt16 => "ushort",
        DataType.Int32 => "int",
        DataType.UInt32 => "uint",
        DataType.Int64 => "long",
        DataType.UInt64 => "ulong",
        DataType.Single => "float",
        DataType.Double => "double",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetEqualFunction(this GeneratorConfig config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Equals(value, {variable})";

        return $"value.Equals({variable})";
    }

    internal static string GetCompareFunction(this GeneratorConfig config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Compare({variable}, value)";

        return $"{variable}.CompareTo(value)";
    }

    internal static string GetHashSource(this GeneratorConfig config, bool seeded) =>
        $"""
             [MethodImpl(MethodImplOptions.AggressiveInlining)]
             public static uint Hash({config.GetTypeName()} value{(seeded ? ", uint seed" : "")}) => {GetHash(config.DataType, seeded)};
         """;

    private static string GetHash(DataType dataType, bool seeded)
    {
        if (dataType == DataType.String)
            return seeded ? "HashHelper.HashStringSeed(value, seed)" : "HashHelper.HashString(value)";

        //For these types, we can use identity hashing
        return dataType switch
        {
            DataType.Char
                or DataType.SByte
                or DataType.Byte
                or DataType.Int16
                or DataType.UInt16
                or DataType.Int32
                or DataType.UInt32 => seeded ? "unchecked((uint)(value ^ seed))" : "unchecked((uint)value)",
            _ => seeded ? "unchecked((uint)(value.GetHashCode() ^ seed))" : "unchecked((uint)(value.GetHashCode()))"
        };
    }
}