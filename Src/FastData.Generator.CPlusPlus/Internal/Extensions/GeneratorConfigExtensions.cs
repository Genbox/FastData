using Genbox.FastData.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

internal static class GeneratorConfigExtensions
{
    internal static string GetTypeName(this GeneratorConfig config, bool asReference = true) => config.DataType switch
    {
        DataType.String when asReference => "std::string&",
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

    internal static string GetHashSource(this GeneratorConfig config)
    {
        if (config.DataType == DataType.String)
        {
            return """
                       static uint32_t get_hash(const std::string& value)
                       {
                           uint32_t hash = 352654597;

                           const char_t* ptr = value.data();
                           size_t len = value.size();

                           while (len-- > 0) {
                               hash = (((hash << 5) | (hash >> 27)) + hash) ^ *ptr;
                               ptr++;
                           }

                           return 352654597 + (hash * 1566083941);
                       }
                   """;
        }

        if (config.DataType.IsIdentityHash() || config.DataType == DataType.Boolean)
        {
            return $$"""
                      static uint32_t get_hash({{config.GetTypeName()}} value)
                      {
                          return static_cast<uint32_t>(value);
                      }
                      """;
        }

        if (config.DataType is DataType.Int64 or DataType.UInt64)
        {
            return $$"""
                   static uint32_t get_hash({{config.GetTypeName()}} value)
                   {
                       return static_cast<uint32_t>(static_cast<int32_t>(value) ^ static_cast<int32_t>(value >> 32));
                   }
                   """;
        }

        if (config.DataType == DataType.Single)
        {
            return $$"""
                          static uint32_t get_hash({{config.GetTypeName()}} value)
                          {
                              uint32_t bits;
                              std::memcpy(&bits, &value, sizeof(bits));
                              if (((bits - 1) & ~0x80000000u) >= 0x7F800000u)
                                  bits &= 0x7F800000u;
                              return bits;
                          }
                      """;
        }

        if (config.DataType == DataType.Double)
        {
            return $$"""
                          static uint32_t get_hash({{config.GetTypeName()}} value)
                          {
                              uint64_t bits;
                              std::memcpy(&bits, &value, sizeof(bits));
                              if (((bits - 1) & ~0x8000000000000000ull) >= 0x7FF0000000000000ull)
                                  bits &= 0x7FF0000000000000ull;
                              return static_cast<uint32_t>(bits) ^ static_cast<uint32_t>(bits >> 32);
                          }
                      """;
        }

        throw new InvalidOperationException("Unsupported data type: " + config.DataType);
    }
}