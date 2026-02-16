using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BloomFilterCode<TKey>(BloomFilterContext ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier(true)}}std::array<uint64_t, {{ctx.BitSet.Length.ToStringInvariant()}}> bloom = {
          {{FormatColumns(ctx.BitSet, ToValueLabel)}}
              };

          {{HashSource}}

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} {{InputKeyName}}){{PostMethodModifier}} {
          {{GetMethodHeader(MethodType.Contains)}}

                  const {{HashSizeType}} hash = get_hash({{LookupKeyName}});
                  const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BitSet.Length)}};
                  const uint32_t shift1 = static_cast<uint32_t>(hash) & 63u;
                  const uint32_t shift2 = static_cast<uint32_t>(hash >> 8) & 63u;
                  const uint64_t mask = (1ULL << shift1) | (1ULL << shift2);
                  return (bloom[index] & mask) == mask;
              }
          """;
}