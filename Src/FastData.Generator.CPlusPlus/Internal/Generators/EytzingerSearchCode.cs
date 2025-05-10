using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : OutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}std::array<{{GetTypeName()}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{GetTypeName()}} value) noexcept
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