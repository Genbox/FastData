namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}E[] _entries = {
          {{FormatColumns(ctx.Data, Render)}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  uint hash = Murmur_32(Hash(value) ^ {{ctx.Seed}});
                  uint index = {{cfg.GetModFunction(ctx.Data.Length)}};
                  ref E entry = ref _entries[index];

                  return hash == entry.HashCode && {{genCfg.GetEqualFunction("entry.Value")}};
              }

          {{genCfg.GetHashSource()}}

              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static uint Murmur_32(uint h)
              {
                  unchecked
                  {
                      h ^= h >> 16;
                      h *= 0x85EBCA6BU;
                      h ^= h >> 13;
                      h *= 0xC2B2AE35U;
                      h ^= h >> 16;
                      return h;
                  }
              }

              [StructLayout(LayoutKind.Auto)]
              private struct E
              {
                  internal E({{genCfg.GetTypeName()}} value, uint hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  internal {{genCfg.GetTypeName()}} Value;
                  internal uint HashCode;
              }
          """;

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) => sb.Append("new E(").Append(ToValueLabel(obj.Key)).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
}