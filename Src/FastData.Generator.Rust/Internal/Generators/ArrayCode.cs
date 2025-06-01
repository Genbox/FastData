using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}const ENTRIES: [{{TypeName}}; {{ctx.Data.Length.ToStringInvariant()}}] = [
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
          {{EarlyExits}}

                  for entry in Self::ENTRIES.iter() {
                      if {{GetEqualFunction("*entry", "value")}} {
                          return true;
                      }
                  }
                  false
              }
          """;
}