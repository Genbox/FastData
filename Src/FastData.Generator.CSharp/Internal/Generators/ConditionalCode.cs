using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate(ReadOnlySpan<T> data) => cfg.ConditionalBranchType switch
    {
        BranchType.If => GenerateIf(data),
        BranchType.Switch => GenerateSwitch(data),
        _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
    };

    private string GenerateIf(ReadOnlySpan<T> data) =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  if ({{FormatList(data, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;

    private string GenerateSwitch(ReadOnlySpan<T> data) =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  switch (value)
                  {
          {{FormatList(data, x => $"            case {ToValueLabel(x)}:", "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;
}