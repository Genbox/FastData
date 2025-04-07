using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config) => config.DataType switch
    {
        KnownDataType.String => "std::string",
        KnownDataType.Boolean => "bool",
        KnownDataType.SByte => "int8_t",
        KnownDataType.Byte => "uint8_t",
        KnownDataType.Char => "char",
        KnownDataType.Int16 => "int16_t",
        KnownDataType.UInt16 => "uint16_t",
        KnownDataType.Int32 => "int32_t",
        KnownDataType.UInt32 => "uint32_t",
        KnownDataType.Int64 => "int64_t",
        KnownDataType.UInt64 => "uint64_t",
        KnownDataType.Single => "float",
        KnownDataType.Double => "double",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetEqualFunction(this GeneratorConfig config, string variable)
    {
        return $"value == {variable}";
    }

    internal static string GetCompareFunction(this GeneratorConfig config, string variable)
    {
        return $"{variable}.compare(value)";
    }

    internal static string GetHashSource(this GeneratorConfig config, bool seeded)
    {
        if (seeded)
        {
            return """
                       static uint32_t get_hash(const std::string& str, uint32_t seed)
                       {
                           uint32_t hash1 = seed;
                           uint32_t hash2 = seed;

                           const char* ptr = str.data();
                           uint32_t length = static_cast<uint32_t>(str.size());

                           auto ptr32 = reinterpret_cast<const uint32_t*>(ptr);
                           while (length >= 4) {
                               hash1 = (hash1 << 5 | hash1 >> (32 - 5)) + hash1 ^ ptr32[0];
                               hash2 = (hash2 << 5 | hash2 >> (32 - 5)) + hash2 ^ ptr32[1];
                               ptr32 += 2;
                               length -= 4;
                           }

                           auto ptr_char = reinterpret_cast<const char*>(ptr32);
                           while (length-- > 0) {
                               hash2 = (hash2 << 5 | hash2 >> (32 - 5)) + hash2 ^ *ptr_char++;
                           }

                           return hash1 + (hash2 * 0x5D588B65);
                       }
                   """;
        }

        return """
                   static uint32_t get_hash(const std::string& str)
                   {
                       uint32_t hash1 = (5381 << 16) + 5381;
                       uint32_t hash2 = (5381 << 16) + 5381;

                       const char* ptr = str.data();
                       uint32_t length = static_cast<uint32_t>(str.size());

                       auto ptr32 = reinterpret_cast<const uint32_t*>(ptr);
                       while (length >= 4) {
                           hash1 = (hash1 << 5 | hash1 >> (32 - 5)) + hash1 ^ ptr32[0];
                           hash2 = (hash2 << 5 | hash2 >> (32 - 5)) + hash2 ^ ptr32[1];
                           ptr32 += 2;
                           length -= 4;
                       }

                       auto ptr_char = reinterpret_cast<const char*>(ptr32);
                       while (length-- > 0) {
                           hash2 = (hash2 << 5 | hash2 >> (32 - 5)) + hash2 ^ *ptr_char++;
                       }

                       return hash1 + (hash2 * 0x5D588B65);
                   }
               """;
    }

    private static string GetHash(KnownDataType dataType, bool seeded)
    {
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