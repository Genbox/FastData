using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class EytzingerSearchCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, EytzingerSearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} {{genCfg.DataType}}[] _entries = new {{genCfg.DataType}}[] {
          {{JoinValues(ctx.Data, Render, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  int i = 0;
                  while (i < _entries.Length)
                  {
                      int comparison = {{genCfg.GetCompareFunction("_entries[i]", "value")}};

                      if (comparison == 0)
                          return true;

                      if (comparison < 0)
                          i = 2 * i + 2;
                      else
                          i = 2 * i + 1;
                  }

                  return false;
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
}