namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} E[] _entries = {
          {{FormatColumns(ctx.Data, Render)}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  uint hash = Hash(value, {{ctx.Seed}});
                  uint index = {{cfg.GetModFunction(ctx.Data.Length)}};
                  ref E entry = ref _entries[index];

                  return hash == entry.HashCode && {{genCfg.GetEqualFunction("entry.Value")}};
              }

          {{genCfg.GetHashSource(true)}}

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