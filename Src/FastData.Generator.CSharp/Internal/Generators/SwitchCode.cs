using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SwitchCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, SwitchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  switch (value)
                  {
          {{JoinValues(ctx.Data, Render, "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append($"            case {ToValueLabel(obj)}:");
}