using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}const KEYS: [{{KeyTypeName}}; {{ctx.Keys.Length.ToStringInvariant()}}] = [
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              ];

              {{MethodAttribute}}
              {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
          {{EarlyExits}}

                  for entry in Self::KEYS.iter() {
                      if {{GetEqualFunction("*entry", "key")}} {
                          return true;
                      }
                  }
                  false
              }
          """;
}