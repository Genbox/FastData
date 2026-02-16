using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BloomFilterCode<TKey>(BloomFilterContext ctx) : RustOutputWriter<TKey>
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}BLOOM: [u64; {{ctx.BitSet.Length.ToStringInvariant()}}] = [
          {{FormatColumns(ctx.BitSet, ToValueLabel)}}
              ];

          {{HashSource}}

              {{MethodAttribute}}
              {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(!typeof(TKey).IsPrimitive)}}) -> bool {
          {{GetMethodHeader(MethodType.Contains)}}

                  let hash = unsafe { Self::get_hash({{LookupKeyName}}) };
                  let index = {{GetModFunction("hash", (ulong)ctx.BitSet.Length)}};
                  let shift1 = (hash as u32) & 63;
                  let shift2 = ((hash >> 8) as u32) & 63;
                  let mask = (1u64 << shift1) | (1u64 << shift2);
                  (Self::BLOOM[index as usize] & mask) == mask
              }
          """;
}