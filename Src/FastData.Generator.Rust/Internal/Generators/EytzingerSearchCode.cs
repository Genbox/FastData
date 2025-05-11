using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}const ENTRIES: [{{TypeName}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
          {{GetEarlyExits()}}

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