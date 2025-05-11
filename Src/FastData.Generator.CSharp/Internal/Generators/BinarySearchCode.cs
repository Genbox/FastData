using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx) : CSharpOutputWriter<T>
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

                  int lo = 0;
                  int hi = {{(ctx.Data.Length - 1).ToStringInvariant()}};
                  while (lo <= hi)
                  {
                      int i = lo + ((hi - lo) >> 1);
                      int order = {{GetCompareFunction("_entries[i]", "value")}};

                      if (order == 0)
                          return true;
                      if (order < 0)
                          lo = i + 1;
                      else
                          hi = i - 1;
                  }

                  return ~lo >= 0;
              }
          """;
}