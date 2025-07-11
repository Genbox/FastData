using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashTableChainCode<TKey, TValue>(HashTableChainContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              struct e
              {
                  {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                  {{KeyTypeName}} value;

                  e({{(ctx.StoreHashCode ? $"const {HashSizeType} hash_code, " : "")}}const {{GetSmallestSignedType(ctx.Buckets.Length)}} next, const {{KeyTypeName}} value)
                     : {{(ctx.StoreHashCode ? "hash_code(hash_code), " : "")}}next(next), value(value) {}
              };

              {{FieldModifier}}std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length.ToStringInvariant()}}> buckets = {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length.ToStringInvariant()}}> entries = {
          {{FormatColumns(ctx.Entries, x => $"e({(ctx.StoreHashCode ? $"{x.Hash.ToStringInvariant()}, " : "")}{x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

          {{HashSource}}

          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  const {{HashSizeType}} hash = get_hash(value);
                  const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = buckets[index] - static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(1);

                  while (i >= 0)
                  {
                      const auto& [{{(ctx.StoreHashCode ? "hash_code, " : "")}}next, value1] = entries[i];

                      if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash_code", "hash")} && " : "")}}{{GetEqualFunction("value1", "value")}})
                          return true;

                      i = next;
                  }

                  return false;
              }
          """;
}