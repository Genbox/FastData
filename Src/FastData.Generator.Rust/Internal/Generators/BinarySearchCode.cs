using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode<T>(GeneratorConfig<T> genCfg, RustCodeGeneratorConfig cfg, BinarySearchContext<T> ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName(true)}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  let mut lo: usize = 0;
                  let mut hi: usize = {{(ctx.Data.Length - 1).ToStringInvariant()}};
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