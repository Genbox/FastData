using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}const ENTRIES: [{{TypeName}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              #[must_use]
              {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
          {{GetEarlyExits()}}

                  for entry in Self::ENTRIES.iter() {
                      if *entry == value {
                          return true;
                      }
                  }
                  false
              }
          """;
}