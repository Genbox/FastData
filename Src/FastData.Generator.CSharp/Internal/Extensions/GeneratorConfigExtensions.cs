using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetEqualFunction(this GeneratorConfig config, string variable1, string variable2)
    {
        if (config.DataType == KnownDataType.String)
            return $"StringComparer.{config.StringComparison}.Equals({variable1}, {variable2})";

        return $"{variable1}.Equals({variable2})";
    }

    internal static string GetCompareFunction(this GeneratorConfig config, string variable1, string variable2)
    {
        if (config.DataType == KnownDataType.String)
            return $"StringComparer.{config.StringComparison}.Compare({variable1}, {variable2})";

        return $"{variable1}.CompareTo({variable2})";
    }

    internal static string GetHashSource(this GeneratorConfig config, bool seeded) =>
        $"""
             [MethodImpl(MethodImplOptions.AggressiveInlining)]
             public static uint Hash({config.DataType} value{(seeded ? ", uint seed" : "")}) => {GetHash(config.DataType, seeded)};
         """;

    private static string GetHash(KnownDataType dataType, bool seeded)
    {
        if (dataType == KnownDataType.String)
            return seeded ? "HashHelper.HashStringSeed(value, seed)" : "HashHelper.HashString(value)";

        //For these types, we can use identity hashing
        return dataType switch
        {
            KnownDataType.Char
                or KnownDataType.SByte
                or KnownDataType.Byte
                or KnownDataType.Int16
                or KnownDataType.UInt16
                or KnownDataType.Int32
                or KnownDataType.UInt32 => seeded ? "unchecked((uint)(value ^ seed))" : "unchecked((uint)value)",
            _ => seeded ? "unchecked((uint)(value.GetHashCode() ^ seed))" : "unchecked((uint)(value.GetHashCode()))"
        };
    }
}