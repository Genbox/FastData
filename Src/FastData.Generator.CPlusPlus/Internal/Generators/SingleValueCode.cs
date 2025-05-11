using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<T>(SingleValueContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public override string Generate() =>
        $$"""
          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}constexpr bool contains(const {{TypeName}} value) noexcept
              {
                  return {{GetEqualFunction("value", ToValueLabel(ctx.Item))}};
              }
          """;
}