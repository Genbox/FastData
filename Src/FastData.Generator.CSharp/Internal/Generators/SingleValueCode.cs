using Genbox.FastData.Generator.CSharp.Internal.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode<T>(SingleValueContext<T> ctx) : CSharpOutputWriter<T>
{
    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public override string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{GetTypeName()}} value)
              {
                  return {{GetEqualFunction("value", ToValueLabel(ctx.Item))}};
              }
          """;
}