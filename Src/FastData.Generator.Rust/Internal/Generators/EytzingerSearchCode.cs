using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode<TKey, TValue>(EytzingerSearchContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}const ENTRIES: [{{KeyTypeName}}; {{ctx.Keys.Length}}] = [
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{KeyTypeName}}) -> bool {
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