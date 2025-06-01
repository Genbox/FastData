using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode<T>(SingleValueContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
                   {{GetEqualFunction(ToValueLabel(ctx.Item), "value")}}
              }
          """;
}