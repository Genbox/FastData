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
        //TODO: Support other types

        return seeded
            ? """
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
              """
            : """
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

    private static string GetHash(DataType dataType, bool seeded)
    {
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