namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, BinarySearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName(true)}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              ];

              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  let mut lo: usize = 0;
                  let mut hi: usize = {{(ctx.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                  while lo <= hi {
                      let i = lo + ((hi - lo) >> 1);
                      let entry = Self::ENTRIES[i];

                      if entry == value {
                          return true;
                      }
                      if entry < value {
                          lo = i + 1;
                      } else {
                          hi = i - 1;
                      }
                  }

                  return false;
              }
          """;
}