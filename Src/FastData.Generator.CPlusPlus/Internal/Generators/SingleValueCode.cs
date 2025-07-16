using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values, className)
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}constexpr bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
              {
                  return {{GetEqualFunction("key", ToValueLabel(ctx.Item))}};
              }
          """;
}