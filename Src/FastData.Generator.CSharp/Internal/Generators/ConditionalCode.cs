using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  if ({{JoinValues(ctx.Data, (x, y) => Render(genCfg, x, y), " || ")}})
                      return true;

                  return false;
              }
          """;

    private static void Render(GeneratorConfig genCfg, StringBuilder sb, object obj) => sb.Append(genCfg.GetEqualFunction("value", ToValueLabel(obj)));
}