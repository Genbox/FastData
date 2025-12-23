using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class RangeCode<TKey, TValue>(RangeContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}fn contains(key: {{GetKeyTypeName(!typeof(TKey).IsPrimitive)}}) -> bool {
                  return key >= {{ctx.Min}} && key <= {{ctx.Max}};
              }
          """;
}