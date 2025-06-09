using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}std::array<{{TypeName}}, {{data.Length.ToStringInvariant()}}> entries = {
          {{FormatColumns(data, ToValueLabel)}}
              };

          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  for ({{ArraySizeType}} i = 0; i < {{data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("entries[i]", "value")}})
                         return true;
                  }
                  return false;
              }
          """;
}