using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, HashSetChainContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              struct E
              {
                  uint32_t hash_code;
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                  {{genCfg.GetTypeName()}} value;

                  E(const uint32_t hash_code, const {{GetSmallestSignedType(ctx.Buckets.Length)}} next, const {{genCfg.GetTypeName()}}& value)
                     : hash_code(hash_code), next(next), value(value) {}
              };

              {{cfg.GetFieldModifier()}} std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, static (sb, x) => sb.Append(x))}}
               };

              {{cfg.GetFieldModifier()}} std::array<E, {{ctx.Entries.Length}}> entries = {
          {{FormatColumns(ctx.Entries, RenderEntry)}}
              };

          {{genCfg.GetHashSource(false)}}

          public:
              {{cfg.GetMethodModifier()}} bool contains(const {{genCfg.GetTypeName()}}& value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  const uint32_t hash = get_hash(value);
                  const uint32_t index = {{cfg.GetModFunction(ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(buckets[index] - 1);

                  while (i >= 0)
                  {
                      const E& entry = entries[i];

                      if (entry.hash_code == hash && {{genCfg.GetEqualFunction("entry.value")}})
                          return true;

                      i = entry.next;
                  }

                  return false;
              }
          """;

    private static void RenderEntry(StringBuilder sb, HashSetEntry x) => sb.Append("E(").Append(x.Hash).Append(", ").Append(x.Next).Append(", ").Append(ToValueLabel(x.Value)).Append(')');
}