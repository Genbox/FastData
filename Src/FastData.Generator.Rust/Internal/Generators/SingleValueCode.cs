namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, SingleValueContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              #[must_use]
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                  return {{ToValueLabel(ctx.Item)}} == value;
              }
          """;
}