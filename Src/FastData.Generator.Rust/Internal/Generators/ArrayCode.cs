namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName()}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  for entry in Self::ENTRIES.iter() {
                      if *entry == value {
                          return true;
                      }
                  }
                  false
              }
          """;
}