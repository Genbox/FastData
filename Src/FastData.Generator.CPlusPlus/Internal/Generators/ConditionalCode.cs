using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<T>(ConditionalContext<T> ctx) : OutputWriter<T>
{
    public override string Generate() =>
        $$"""
          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{GetTypeName()}} value) noexcept
              {
          {{GetEarlyExits()}}

                  if ({{FormatList(ctx.Data, x => $"{GetEqualFunction("value",ToValueLabel(x))}", " || ")}})
                      return true;

                  return false;
              }
          """;
}