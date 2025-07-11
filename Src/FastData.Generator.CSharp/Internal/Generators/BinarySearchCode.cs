using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}{{KeyTypeName}}[] _entries = new {{KeyTypeName}}[] {
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} value)
              {
          {{EarlyExits}}

                  int lo = 0;
                  int hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
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