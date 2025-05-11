using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              struct e
              {
                  {{TypeName}} value;
                  uint32_t hash_code;

                  e(const {{TypeName}} value, const uint32_t hash_code)
                  : value(value), hash_code(hash_code) {}
              };

              {{GetFieldModifier(false)}}std::array<e, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, x => $"e({ToValueLabel(x.Key)}, {ToValueLabel(x.Value)})")}}
              };

          {{GetHashSource()}}

              static uint32_t murmur_32(uint32_t h) noexcept
              {
                  h ^= h >> 16;
                  h *= 0x85EBCA6BU;
                  h ^= h >> 13;
                  h *= 0xC2B2AE35U;
                  h ^= h >> 16;
                  return h;
              }

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}
                  const uint32_t hash = murmur_32(get_hash(value) ^ {{ctx.Seed}});
                  const size_t index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                  const e& entry = entries[index];

                  return {{GetEqualFunction("hash", "entry.hash_code")}} && {{GetEqualFunction("value", "entry.value")}};
              }
          """;
}