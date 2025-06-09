using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}const ENTRIES: [{{TypeName}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
          {{EarlyExits}}

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