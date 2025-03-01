using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SingleValueCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate() => true;

    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
                  return {{GetEqualFunction(config.DataType, "value", ToValueLabel(config.Data[0]))}};
              }
          """;
}