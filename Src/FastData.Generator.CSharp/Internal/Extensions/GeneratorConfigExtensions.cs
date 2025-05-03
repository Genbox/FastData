using Genbox.FastData.Extensions;
using Genbox.FastData.Specs.Hash;

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

    internal static string GetHashSource(this GeneratorConfig config)
    {
        if (config.DataType == DataType.String)
        {
            return $$"""
                         [MethodImpl(MethodImplOptions.AggressiveInlining)]
                         private static uint Hash({{config.GetTypeName()}} value)
                         {
                              uint hash1 = 352654597;
                              uint hash2 = 352654597;

                              ref char ptr = ref MemoryMarshal.GetReference(value.AsSpan());
                              int len = value.Length;

                              while (len-- > 0)
                              {
                                  hash2 = (((hash2 << 5) | (hash2 >> 27)) + hash2) ^ ptr;
                                  ptr = ref Unsafe.Add(ref ptr, 1);
                              }

                              return hash1 + (hash2 * 1566083941);
                         }
                     """;
        }

        return $$"""
                     [MethodImpl(MethodImplOptions.AggressiveInlining)]
                     private static uint Hash({{config.GetTypeName()}} value)
                     {
                         return unchecked((uint)(value{{(config.DataType.IsIdentityHash() ? "" : ".GetHashCode()")}}));
                     }
                 """;
    }
}