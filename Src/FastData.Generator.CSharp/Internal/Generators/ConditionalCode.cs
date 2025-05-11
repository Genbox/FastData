using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>
{
    public override string Generate() => cfg.ConditionalBranchType switch
    {
        BranchType.If => GenerateIf(),
        BranchType.Switch => GenerateSwitch(),
        _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
    };

    private string GenerateIf() =>
        $$"""
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{GetTypeName()}} value)
              {
          {{GetEarlyExits()}}

                  if ({{FormatList(ctx.Data, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;

    private string GenerateSwitch() =>
        $$"""
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{GetTypeName()}} value)
              {
          {{GetEarlyExits()}}

                  switch (value)
                  {
          {{FormatList(ctx.Data, x => $"            case {ToValueLabel(x)}:", "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;
}