using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}std::array<{{TypeName}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}

                  for ({{GetArraySizeType()}} i = 0; i < {{ctx.Data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("entries[i]", "value")}})
                         return true;
                  }
                  return false;
              }
          """;
}