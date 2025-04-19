using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config) => config.DataType switch
    {
        DataType.String => "std::string",
        DataType.Boolean => "bool",
        DataType.SByte => "int8_t",
        DataType.Byte => "uint8_t",
        DataType.Char => "char",
        DataType.Int16 => "int16_t",
        DataType.UInt16 => "uint16_t",
        DataType.Int32 => "int32_t",
        DataType.UInt32 => "uint32_t",
        DataType.Int64 => "int64_t",
        DataType.UInt64 => "uint64_t",
        DataType.Single => "float",
        DataType.Double => "double",
        _ => throw new InvalidOperationException("Invalid DataType: " + config.DataType)
    };

    internal static string GetEqualFunction(this GeneratorConfig config, string variable) => $"value == {variable}";

    internal static string GetCompareFunction(this GeneratorConfig config, string variable) => $"{variable}.compare(value)";

    internal static string GetHashSource(this GeneratorConfig config, bool seeded)
    {
        if (config.DataType == DataType.String)
        {
            return $$"""
                         static uint32_t get_hash(const std::string& value{{(seeded ? ", uint32_t seed," : "")}})
                         {
                             uint32_t hash1 = {{(seeded ? "seed" : "(5381 << 16) + 5381")}};
                             uint32_t hash2 = {{(seeded ? "seed" : "(5381 << 16) + 5381")}};

                             const char* ptr = value.data();
                             uint32_t length = static_cast<uint32_t>(value.size());

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

        return $$"""
                     static uint32_t get_hash(const {{config.GetTypeName()}} value{{(seeded ? ", uint32_t seed," : "")}})
                     {
                         return {{(seeded ? "reinterpret_cast<uint32_t>(value ^ seed)" : "reinterpret_cast<uint32_t>(value)")}};
                     }
                 """;
    }
}