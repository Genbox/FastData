using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}const ENTRIES: [{{TypeName}}; {{data.Length.ToStringInvariant()}}] = [
          {{FormatColumns(data, ToValueLabel)}}
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