namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<T>(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, ArrayContext<T> ctx) : IOutputWriter
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