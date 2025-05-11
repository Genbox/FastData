using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : CPlusPlusOutputWriter<T>
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