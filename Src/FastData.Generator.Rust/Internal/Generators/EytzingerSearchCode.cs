using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class EytzingerSearchCode<TKey, TValue>(EytzingerSearchContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}const KEYS: [{{KeyTypeName}}; {{ctx.Keys.Length}}] = [
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
          {{EarlyExits}}

                  let mut i: usize = 0;
                  while i < Self::KEYS.len() {
                      let entry = Self::KEYS[i];

                      if entry == key {
                          return true;
                      }
                      if entry < key {
                          i = 2 * i + 2;
                      } else {
                          i = 2 * i + 1;
                      }
                  }

                  false
              }
          """;
}