using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetChainCode<T>(HashSetChainContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              struct e
              {
                  uint64_t hash_code;
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                  {{TypeName}} value;

                  e(const uint64_t hash_code, const {{GetSmallestSignedType(ctx.Buckets.Length)}} next, const {{TypeName}} value)
                     : hash_code(hash_code), next(next), value(value) {}
              };

              {{GetFieldModifier()}}std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length}}> entries = {
          {{FormatColumns(ctx.Entries, x => $"e({x.Hash.ToStringInvariant()}, {x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

          {{GetHashSource()}}

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}

                  const uint64_t hash = get_hash(value);
                  const size_t index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = buckets[index] - 1;

                  while (i >= 0)
                  {
                      const auto& [hash_code, next, value1] = entries[i];

                      if ({{GetEqualFunction("hash_code", "hash")}} && {{GetEqualFunction("value1", "value")}})
                          return true;

                      i = next;
                  }

                  return false;
              }
          """;
}