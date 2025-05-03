namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, SingleValueContext ctx) : IOutputWriter
{
    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public string Generate() =>
        $$"""
          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}constexpr bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
                  return value == {{ToValueLabel(ctx.Item)}};
              }
          """;
}