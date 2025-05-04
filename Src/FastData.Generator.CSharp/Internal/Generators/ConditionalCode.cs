using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, ConditionalContext ctx) : IOutputWriter
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
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  if ({{FormatList(ctx.Data, x => genCfg.GetEqualFunction(ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;

    private string GenerateSwitch() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  switch (value)
                  {
          {{FormatList(ctx.Data, static x => $"            case {ToValueLabel(x)}:", "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;
}