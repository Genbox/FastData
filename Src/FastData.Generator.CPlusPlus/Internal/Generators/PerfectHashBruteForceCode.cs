using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              struct E
              {
                  {{genCfg.GetTypeName()}} value;
                  uint32_t hash_code;

                  E(const {{genCfg.GetTypeName()}}& value, const uint32_t hash_code)
                  : value(value), hash_code(hash_code) {}
              };

              {{cfg.GetFieldModifier()}} std::array<E, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, Render)}}
              };

          {{genCfg.GetHashSource(true)}}

          public:
              {{cfg.GetMethodModifier()}} bool contains(const {{genCfg.GetTypeName()}}& value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  const uint32_t hash = get_hash(value, {{ctx.Seed}});
                  const uint32_t index = {{cfg.GetModFunction(ctx.Data.Length)}};
                  const E& entry = entries[index];

                  return hash == entry.hash_code && {{genCfg.GetEqualFunction("entry.value")}};
              }
          """;

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) => sb.Append("E(").Append(ToValueLabel(obj.Key)).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
}