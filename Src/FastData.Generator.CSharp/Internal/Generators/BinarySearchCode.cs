using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}{{TypeName}}[] _entries = new {{TypeName}}[] {
          {{FormatColumns(data, ToValueLabel)}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  int lo = 0;
                  int hi = {{(data.Length - 1).ToStringInvariant()}};
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