using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class EytzingerSearchCode<TKey, TValue>(EytzingerSearchContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}{{KeyTypeName}}[] _keys = new {{KeyTypeName}}[] {
          {{FormatColumns(ctx.Keys, ToValueLabel)}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} key)
              {
          {{EarlyExits}}

                  int i = 0;
                  while (i < _keys.Length)
                  {
                      int comparison = {{GetCompareFunction("_keys[i]", "key")}};

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