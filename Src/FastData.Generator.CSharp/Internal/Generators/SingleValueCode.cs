namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode<T>(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, SingleValueContext<T> ctx) : IOutputWriter
{
    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
                  return {{genCfg.GetEqualFunction(ToValueLabel(ctx.Item))}};
              }
          """;
}