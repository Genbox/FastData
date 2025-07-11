using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} value)
              {
                  return {{GetEqualFunction("value", ToValueLabel(ctx.Item))}};
              }
          """;
}