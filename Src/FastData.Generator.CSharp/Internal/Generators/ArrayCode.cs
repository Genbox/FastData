using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
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

                  for (int i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("key", "_keys[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}