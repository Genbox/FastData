using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BinarySearchCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, BinarySearchContext ctx) : IOutputWriter
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

                  int lo = 0;
                  int hi = {{(ctx.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                  while (lo <= hi)
                  {
                      int i = lo + ((hi - lo) >> 1);
                      int order = {{genCfg.GetCompareFunction("_entries[i]", "value")}};

                      if (order == 0)
                          return true;
                      if (order < 0)
                          lo = i + 1;
                      else
                          hi = i - 1;
                  }

                  return ((~lo) >= 0);
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
}