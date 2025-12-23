using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed")]
internal sealed class RangeCode<TKey, TValue>(RangeContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
            public:
              {{MethodAttribute}}
              {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                  return key >= {{ctx.Min}} && key <= {{ctx.Max}};
              }
          """;
}