using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  if ({{FormatList(ctx.Data, x => GetEqualFunction("value", ToValueLabel(x)), " || ")}})
                      return true;

                  return false;
              }
          """;
}