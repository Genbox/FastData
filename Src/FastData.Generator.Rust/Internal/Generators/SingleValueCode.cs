using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode<T>(SingleValueContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              #[must_use]
              {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                  {{ToValueLabel(ctx.Item)}} == value
              }
          """;
}