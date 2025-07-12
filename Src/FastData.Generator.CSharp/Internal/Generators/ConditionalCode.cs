using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() => cfg.ConditionalBranchType switch
    {
        BranchType.If => GenerateIf(ctx.Keys),
        BranchType.Switch => GenerateSwitch(ctx.Keys),
        _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
    };

    private string GenerateIf(ReadOnlySpan<TKey> data) =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} key)
              {
          {{EarlyExits}}

                  if ({{FormatList(data, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;

    private string GenerateSwitch(ReadOnlySpan<TKey> data) =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} key)
              {
          {{EarlyExits}}

                  switch (key)
                  {
          {{FormatList(data, x => $"            case {ToValueLabel(x)}:", "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;
}