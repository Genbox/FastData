namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode<T>(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, SingleValueContext<T> ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              #[must_use]
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                  {{ToValueLabel(ctx.Item)}} == value
              }
          """;
}