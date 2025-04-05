using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} {{genCfg.GetTypeName()}}[] _entries = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  for (int i = 0; i < {{ctx.Data.Length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                  {
                      if ({{genCfg.GetEqualFunction("value", "_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}