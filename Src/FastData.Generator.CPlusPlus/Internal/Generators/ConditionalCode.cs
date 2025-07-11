using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  if ({{FormatList(ctx.Keys, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;
}