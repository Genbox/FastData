using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
          {{EarlyExits}}

                  if {{FormatList(data, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}} {
                      return true;
                  }

                  false
              }
          """;
}