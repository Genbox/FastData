using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class RangeCode<TKey>(RangeContext<TKey> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
              {
          {{GetMethodHeader(MethodType.Contains)}}

                  return {{LookupKeyName}} >= {{ctx.Min}} && {{LookupKeyName}} <= {{ctx.Max}};
              }
          """;
}