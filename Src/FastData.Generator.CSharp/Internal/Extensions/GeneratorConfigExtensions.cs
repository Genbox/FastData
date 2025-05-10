using Genbox.FastData.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName<T>(this GeneratorConfig<T> config) => config.DataType switch
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

    internal static string GetEqualFunction<T>(this GeneratorConfig<T> config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Equals(value, {variable})";

        return $"value.Equals({variable})";
    }

    internal static string GetCompareFunction<T>(this GeneratorConfig<T> config, string variable)
    {
        if (config.DataType == DataType.String)
            return $"StringComparer.{config.StringComparison}.Compare({variable}, value)";

        return $"{variable}.CompareTo(value)";
    }

    internal static string GetHashSource<T>(this GeneratorConfig<T> config) =>
        $$"""
              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static uint Hash({{config.GetTypeName()}} value)
              {
          {{GetHash(config.DataType)}}
              }
          """;

    private static string GetHash(DataType type)
    {
        if (type == DataType.String)
        {
            return """
                            uint hash = 352654597;

                            ref char ptr = ref MemoryMarshal.GetReference(value.AsSpan());
                            int len = value.Length;

                            while (len-- > 0)
                            {
                                hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
                                ptr = ref Unsafe.Add(ref ptr, 1);
                            }

                            return 352654597 + (hash * 1566083941);
                   """;
        }

        return $"        return unchecked((uint)(value{(type.IsIdentityHash() ? "" : ".GetHashCode()")}));";
    }
}