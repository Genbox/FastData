using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              struct e
              {
                  {{TypeName}} value;
                  {{HashType}} hash_code;

                  e(const {{TypeName}} value, const {{HashType}} hash_code)
                  : value(value), hash_code(hash_code) {}
              };

              {{GetFieldModifier(false)}}std::array<e, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, x => $"e({ToValueLabel(x.Key)}, {x.Value.ToStringInvariant()})")}}
              };

          {{GetHashSource()}}

          {{GetMixer()}}

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}
                  const {{HashType}} hash = mixer(get_hash(value) ^ {{ctx.Seed}});
                  const size_t index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                  const e& entry = entries[index];

                  return {{GetEqualFunction("hash", "entry.hash_code")}} && {{GetEqualFunction("value", "entry.value")}};
              }
          """;

    private string GetMixer()
    {
        if (GeneratorConfig.Use64BitHashing)
        {
            return """
                       static uint64 mixer(uint64 h) noexcept
                       {
                           h ^= h >> 33;
                           h *= 0xFF51AFD7ED558CCD;
                           h ^= h >> 33;
                           h *= 0xC4CEB9FE1A85EC53;
                           h ^= h >> 33;
                           return h;
                       }
                   """;
        }

        return """
                   static uint32_t mixer(uint32_t h) noexcept
                   {
                       h ^= h >> 16;
                       h *= 0x85EBCA6BU;
                       h ^= h >> 13;
                       h *= 0xC2B2AE35U;
                       h ^= h >> 16;
                       return h;
                   }
               """;
    }
}