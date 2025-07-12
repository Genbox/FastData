using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}const KEYS: [{{TypeNameWithLifetime}}; {{ctx.Keys.Length.ToStringInvariant()}}] = [
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
          {{EarlyExits}}

                  let mut lo: usize = 0;
                  let mut hi: usize = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                  while lo <= hi {
                      let i = lo + ((hi - lo) >> 1);
                      let entry = Self::KEYS[i];

                      if entry == key {
                          return true;
                      }
                      if entry < key {
                          lo = i + 1;
                      } else {
                          hi = i - 1;
                      }
                  }

                  false
              }
          """;
}