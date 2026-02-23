using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusHashDef : IHashDef
{
    public string GetHashSource(Type keyType, string typeName, HashInfo info)
    {
        TypeCode typeCode = Type.GetTypeCode(keyType);
        bool notConst = typeCode is TypeCode.Single or TypeCode.Double or TypeCode.Int64 or TypeCode.UInt64;

        return $$"""
                 static{{(notConst ? " " : " constexpr ")}}uint64_t get_hash(const {{typeName}} value) noexcept
                 {
                 {{GetHash(keyType, info)}}
                 }
                 """;
    }

    private static string GetHash(Type keyType, HashInfo info)
    {
        TypeCode typeCode = Type.GetTypeCode(keyType);

        if (typeCode == TypeCode.String)
        {
            return """
                       uint64_t hash = 352654597;

                       for (unsigned char ch : value)
                           hash = (((hash << 5) | (hash >> 27)) + hash) ^ static_cast<uint32_t>(ch);

                       return 352654597 + (hash * 1566083941);
                   """;
        }

        if (keyType.IsIdentityHash())
            return "    return static_cast<uint64_t>(value);";

        if (typeCode == TypeCode.Single)
        {
            return info.HasZeroOrNaN
                ? """
                      uint32_t bits;
                      std::memcpy(&bits, &value, sizeof(bits));
                      if (((bits - 1) & ~0x80000000u) >= 0x7F800000u)
                          bits &= 0x7F800000u;
                      return bits;
                  """
                : """
                      uint32_t bits;
                      std::memcpy(&bits, &value, sizeof(bits));
                      return bits;
                  """;
        }

        if (typeCode == TypeCode.Double)
        {
            return info.HasZeroOrNaN
                ? """
                      uint64_t bits;
                      std::memcpy(&bits, &value, sizeof(bits));
                      if (((bits - 1) & ~0x8000000000000000ull) >= 0x7FF0000000000000ull)
                          bits &= 0x7FF0000000000000ull;
                      return bits;
                  """
                : """
                      uint64_t bits;
                      std::memcpy(&bits, &value, sizeof(bits));
                      return bits;
                  """;
        }

        throw new InvalidOperationException("Unsupported data type: " + keyType);
    }
}