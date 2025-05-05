using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetChainCode<T>(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, HashSetChainContext<T> ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              struct e
              {
                  uint32_t hash_code;
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                  {{genCfg.GetTypeName()}} value;

                  e(const uint32_t hash_code, const {{GetSmallestSignedType(ctx.Buckets.Length)}} next, const {{genCfg.GetTypeName()}} value)
                     : hash_code(hash_code), next(next), value(value) {}
              };

              {{cfg.GetFieldModifier()}}std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{cfg.GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length}}> entries = {
          {{FormatColumns(ctx.Entries, static x => $"e({x.Hash}, {x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

          {{genCfg.GetHashSource()}}

          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  const uint32_t hash = get_hash(value);
                  const size_t index = {{cfg.GetModFunction(ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = buckets[index] - 1;

                  while (i >= 0)
                  {
                      const auto& [hash_code, next, value1] = entries[i];

                      if (hash_code == hash && value1 == value)
                          return true;

                      i = next;
                  }

                  return false;
              }
          """;
}