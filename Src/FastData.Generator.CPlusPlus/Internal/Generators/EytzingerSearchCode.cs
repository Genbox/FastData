using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class EytzingerSearchCode<TKey, TValue>(EytzingerSearchContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> entries = {
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              };

          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
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