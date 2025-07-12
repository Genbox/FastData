using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
          {{EarlyExits}}

                  if {{FormatList(ctx.Keys, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}} {
                      return true;
                  }

                  false
              }
          """;
}