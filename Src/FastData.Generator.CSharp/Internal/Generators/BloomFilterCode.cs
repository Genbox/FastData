using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BloomFilterCode<TKey>(BloomFilterContext ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}ulong[] _bloom = new ulong[] {
          {{FormatColumns(ctx.BitSet, ToValueLabel)}}
              };

          {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
              {
          {{GetMethodHeader(MethodType.Contains)}}

                  ulong hash = Hash({{LookupKeyName}});
                  {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BitSet.Length)}};
                  uint shift1 = (uint)hash & 63u;
                  uint shift2 = (uint)(hash >> 8) & 63u;
                  ulong mask = (1UL << (int)shift1) | (1UL << (int)shift2);
                  return (_bloom[index] & mask) == mask;
              }
          """;
}