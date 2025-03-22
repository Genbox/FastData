using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} Entry[] _entries = new Entry[] {
          {{JoinValues(ctx.Data, Render, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint hash = Hash(value, {{ctx.Seed}});
                  uint index = {{cfg.GetModFunction("hash", (uint)ctx.Data.Length)}};
                  ref Entry entry = ref _entries[index];

                  return hash == entry.HashCode && {{genCfg.GetEqualFunction("value", "entry.Value")}};
              }

          {{genCfg.GetHashSource(true)}}

              [StructLayout(LayoutKind.Auto)]
              private struct Entry
              {
                  public Entry({{genCfg.DataType}} value, uint hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  public {{genCfg.DataType}} Value;
                  public uint HashCode;
              }
          """;

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) => sb.Append("        new Entry(").Append(ToValueLabel(obj.Key)).Append(", ").Append(obj.Value).Append("u)");
}