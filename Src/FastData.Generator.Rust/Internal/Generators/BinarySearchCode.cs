using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}const ENTRIES: [{{TypeNameWithLifetime}}; {{data.Length.ToStringInvariant()}}] = [
          {{FormatColumns(data, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
          {{EarlyExits}}

                  let mut lo: usize = 0;
                  let mut hi: usize = {{(data.Length - 1).ToStringInvariant()}};
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

                  false
              }
          """;
}