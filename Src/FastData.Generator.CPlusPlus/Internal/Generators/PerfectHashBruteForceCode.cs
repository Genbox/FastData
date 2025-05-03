namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              struct e
              {
                  {{genCfg.GetTypeName(false)}} value;
                  uint32_t hash_code;

                  e(const {{genCfg.GetTypeName(false)}} value, const uint32_t hash_code)
                  : value(value), hash_code(hash_code) {}
              };

              {{cfg.GetFieldModifier()}}std::array<e, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, Render)}}
              };

          {{genCfg.GetHashSource()}}

              static uint32_t murmur_32(uint32_t h)
              {
                  h ^= h >> 16;
                  h *= 0x85EBCA6BU;
                  h ^= h >> 13;
                  h *= 0xC2B2AE35U;
                  h ^= h >> 16;
                  return h;
              }

          public:
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}
                  const uint32_t hash = murmur_32(get_hash(value) ^ {{ctx.Seed}});
                  const uint32_t index = {{cfg.GetModFunction(ctx.Data.Length)}};
                  const e& entry = entries[index];

                  return hash == entry.hash_code && value == entry.value;
              }
          """;

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) => sb.Append("e(").Append(ToValueLabel(obj.Key)).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
}