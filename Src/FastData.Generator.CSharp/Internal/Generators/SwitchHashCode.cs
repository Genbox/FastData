using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SwitchHashCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, SwitchHashContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  switch (Hash(value))
                  {
          {{JoinValues(ctx.HashCodes, (x, y) => Render(genCfg, x, y), "\n")}}
                  }
                  return false;
              }

          {{genCfg.GetHashSource(false)}}
          """;

    private static void Render(GeneratorConfig genCfg, StringBuilder sb, KeyValuePair<uint, object> obj)
    {
        sb.Append($"""
                               case {obj.Key.ToString(NumberFormatInfo.InvariantInfo)}:
                                    return {genCfg.GetEqualFunction("value", ToValueLabel(obj.Value))};
                   """);
    }
}