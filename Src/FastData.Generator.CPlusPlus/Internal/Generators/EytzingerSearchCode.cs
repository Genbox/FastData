using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : CPlusPlusOutputWriter<T>
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

                  size_t i = 0;
                  while (i < entries.size())
                  {
                      if ({{GetEqualFunction("entries[i]", "value")}})
                          return true;

                      if (entries[i] < value)
                          i = 2 * i + 2;
                      else
                          i = 2 * i + 1;
                  }

                  return false;
              }
          """;
}