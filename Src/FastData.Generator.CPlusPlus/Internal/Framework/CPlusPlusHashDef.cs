using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName, string? stringHash)
    {
        bool notConst = dataType is DataType.Single or DataType.Double or DataType.Int64 or DataType.UInt64;

        return $$"""
                     static{{(notConst ? " " : " constexpr ")}}uint64_t get_hash(const {{typeName}} value) noexcept
                     {
                 {{GetHash(dataType, stringHash)}}
                     }
                 """;
    }

    private static string GetHash(DataType dataType, string? stringHash)
    {
        if (dataType == DataType.String)
        {
            return """
                           uint64_t hash = 352654597;

                           const char* ptr = value.data();
                           size_t len = value.size();

                           while (len-- > 0) {
                               hash = (((hash << 5) | (hash >> 27)) + hash) ^ *ptr;
                               ptr++;
                           }

                           return 352654597 + (hash * 1566083941);
                   """;
        }

        if (dataType.IsIdentityHash() || dataType == DataType.Boolean)
            return "        return static_cast<uint64_t>(value);";

        if (dataType == DataType.Single)
        {
            return """
                           uint32_t bits;
                           std::memcpy(&bits, &value, sizeof(bits));
                           if (((bits - 1) & ~0x80000000u) >= 0x7F800000u)
                               bits &= 0x7F800000u;
                           return bits;
                   """;
        }

        if (dataType == DataType.Double)
        {
            return """
                           uint64_t bits;
                           std::memcpy(&bits, &value, sizeof(bits));
                           if (((bits - 1) & ~0x8000000000000000ull) >= 0x7FF0000000000000ull)
                               bits &= 0x7FF0000000000000ull;
                           return bits;
                   """;
        }

        throw new InvalidOperationException("Unsupported data type: " + dataType);
    }
}