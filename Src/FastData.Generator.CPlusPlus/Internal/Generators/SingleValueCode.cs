using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}constexpr bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
              {
                  return {{GetEqualFunction("value", ToValueLabel(ctx.Item))}};
              }
          """;
}