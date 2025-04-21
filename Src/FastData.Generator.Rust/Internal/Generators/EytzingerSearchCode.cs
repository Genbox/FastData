namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, EytzingerSearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName()}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              ];

              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  let mut i: usize = 0;
                  while i < Self::ENTRIES.len() {
                      let comparison = {{genCfg.GetCompareFunction("Self::ENTRIES[i]")}};

                      if comparison == 0 {
                          return true;
                      }
                      if comparison < 0 {
                          i = 2 * i + 2;
                      } else {
                          i = 2 * i + 1;
                      }
                  }

                  false
              }
          """;
}