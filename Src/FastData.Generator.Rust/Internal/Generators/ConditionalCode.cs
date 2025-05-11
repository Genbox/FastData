using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              #[must_use]
              {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
          {{GetEarlyExits()}}

                  if {{FormatList(ctx.Data, x => $"value == {ToValueLabel(x)}", " || ")}} {
                      return true;
                  }

                  false
              }
          """;
}