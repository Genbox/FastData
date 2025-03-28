using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
{
    public string Generate() => cfg.ConditionalBranchType switch
    {
        BranchType.If => GenerateIf(),
        BranchType.Switch => GenerateSwitch(),
        _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
    };

    private string GenerateIf() =>
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

    private string GenerateSwitch() =>
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
    private static void Render(GeneratorConfig genCfg, StringBuilder sb, object obj) => sb.Append(genCfg.GetEqualFunction("value", ToValueLabel(obj)));
}