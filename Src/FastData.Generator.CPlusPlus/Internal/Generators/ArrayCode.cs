using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}std::array<{{TypeName}}, {{ctx.Data.Length.ToStringInvariant()}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  for ({{ArraySizeType}} i = 0; i < {{ctx.Data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("entries[i]", "value")}})
                         return true;
                  }
                  return false;
              }
          """;
}