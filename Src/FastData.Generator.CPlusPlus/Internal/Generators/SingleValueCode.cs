using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<T>(SingleValueContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}constexpr bool contains(const {{TypeName}} value){{PostMethodModifier}}
              {
                  return {{GetEqualFunction("value", ToValueLabel(ctx.Item))}};
              }
          """;
}