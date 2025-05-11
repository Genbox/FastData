using Genbox.FastData.Generator.CSharp.Internal.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(EytzingerSearchContext<T> ctx) : CSharpOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}{{GetTypeName()}}[] _entries = new {{GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{GetTypeName()}} value)
              {
          {{GetEarlyExits()}}

                  int i = 0;
                  while (i < _entries.Length)
                  {
                      int comparison = {{GetCompareFunction("_entries[i]", "value")}};

                      if (comparison == 0)
                          return true;

                      if (comparison < 0)
                          i = 2 * i + 2;
                      else
                          i = 2 * i + 1;
                  }

                  return false;
              }
          """;
}