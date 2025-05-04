namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, EytzingerSearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName()}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  let mut i: usize = 0;
                  while i < Self::ENTRIES.len() {
                      let entry = Self::ENTRIES[i];

                      if entry == value {
                          return true;
                      }
                      if entry < value {
                          i = 2 * i + 2;
                      } else {
                          i = 2 * i + 1;
                      }
                  }

                  false
              }
          """;
}