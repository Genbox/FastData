using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values, className)
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  if ({{FormatList(ctx.Keys, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;
}