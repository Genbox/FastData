using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx) : RustOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
          {{EarlyExits}}

                  if {{FormatList(ctx.Data, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}} {
                      return true;
                  }

                  false
              }
          """;
}