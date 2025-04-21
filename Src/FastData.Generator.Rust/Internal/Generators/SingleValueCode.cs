namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, SingleValueContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                  return {{genCfg.GetEqualFunction(ToValueLabel(ctx.Item))}};
              }
          """;
}