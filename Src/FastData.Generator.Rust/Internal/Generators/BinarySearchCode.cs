using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}const ENTRIES: [{{GetTypeNameWithLifetime()}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
          {{GetEarlyExits()}}

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