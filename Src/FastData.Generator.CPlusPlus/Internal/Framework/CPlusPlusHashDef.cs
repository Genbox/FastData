using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusHashDef(TypeMap map) : IHashDef, IHashExpressionDef
{
    public string GetStringHashSource(string typeName) =>
        """
        uint64_t hash = 352654597;

           for (unsigned char ch : value)
               hash = (((hash << 5) | (hash >> 27)) + hash) ^ static_cast<uint32_t>(ch);

           return 352654597 + (hash * 1566083941);
        """;

    public string GetNumericHashSource(TypeCode keyType, string typeName, bool hasZeroOrNaN)
    {
        if (keyType.UsesIdentityHash())
            return "    return static_cast<uint64_t>(value);";

        if (keyType == TypeCode.Single)
        {
            return hasZeroOrNaN
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

        if (keyType == TypeCode.Double)
        {
            return hasZeroOrNaN
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

        throw new InvalidOperationException("Unsupported key type: " + keyType);
    }

    public string Wrap(TypeCode keyType, string typeName, string hash)
    {
        bool notConst = keyType is TypeCode.Single or TypeCode.Double or TypeCode.Int64 or TypeCode.UInt64;

        return $$"""
                 static{{(notConst ? " " : " constexpr ")}}uint64_t get_hash(const {{typeName}} value) noexcept
                 {
                 {{hash}}
                 }
                 """;
    }

    public string RenderAdditionalData(AdditionalData[] info)
    {
        StringBuilder sb = new StringBuilder();

        foreach (AdditionalData state in info)
        {
            string typeName = map.GetTypeName(state.Type);
            string length = state.Values.Length.ToString(CultureInfo.InvariantCulture);
            string values = string.Join(", ", state.Values.Cast<object>().Select(x => map.ToValueLabel(x, state.Type)));

            sb.Append("    inline static const std::array<")
              .Append(typeName)
              .Append(", ")
              .Append(length)
              .Append("> ")
              .Append(state.Name)
              .Append(" = { ")
              .Append(values)
              .Append(" };")
              .Append('\n');
        }

        return sb.ToString();
    }

    public string RenderFunctions(ReaderFunctions functions)
    {
        StringBuilder sb = new StringBuilder();

        if (functions.HasFlag(ReaderFunctions.ReadU8))
        {
            sb.AppendLine("    static uint8_t ReadU8(const std::string_view value, const size_t offset) noexcept { return static_cast<uint8_t>(value[offset]); }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU16))
        {
            sb.AppendLine("    static uint16_t ReadU16(const std::string_view value, const size_t offset) noexcept { uint16_t result; std::memcpy(&result, value.data() + offset, sizeof(result)); return result; }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU32))
        {
            sb.AppendLine("    static uint32_t ReadU32(const std::string_view value, const size_t offset) noexcept { uint32_t result; std::memcpy(&result, value.data() + offset, sizeof(result)); return result; }");
        }

        if (functions.HasFlag(ReaderFunctions.ReadU64))
        {
            sb.AppendLine("    static uint64_t ReadU64(const std::string_view value, const size_t offset) noexcept { uint64_t result; std::memcpy(&result, value.data() + offset, sizeof(result)); return result; }");
        }

        return sb.ToString();
    }
}
