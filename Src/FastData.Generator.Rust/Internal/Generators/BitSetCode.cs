using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BitSetCode<TKey, TValue>(BitSetContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($"""
                       {FieldModifier}BITSET: [u64; {ctx.BitSet.Length.ToStringInvariant()}] = [
                   {FormatColumns(ctx.BitSet, ToValueLabel)}
                       ];

                   """);

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""
                          {FieldModifier}VALUES: [{GetValueTypeName(customValue)}; {values.Length.ToStringInvariant()}] = [
                       {FormatColumns(values, ToValueLabel)}
                           ];

                       """);
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            let offset = {{GetOffsetExpression()}};
                            let word = offset >> 6;
                            let mask = 1u64 << ((offset & 63) as u32);
                            (Self::BITSET[word] & mask) != 0
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                let offset = {{GetOffsetExpression()}};
                                let word = offset >> 6;
                                let mask = 1u64 << ((offset & 63) as u32);
                                if (Self::BITSET[word] & mask) == 0 {
                                    return None;
                                }

                                Some(Self::VALUES[offset])
                            }
                        """);
        }

        return sb.ToString();
    }

    private string GetOffsetExpression() => GeneratorConfig.KeyType switch
    {
        KeyType.Char => $"({LookupKeyName} as u32 - Self::MIN_KEY as u32) as usize",
        KeyType.SByte or KeyType.Int16 or KeyType.Int32 or KeyType.Int64 => $"(({LookupKeyName} as i64) - (Self::MIN_KEY as i64)) as usize",
        KeyType.Byte or KeyType.UInt16 or KeyType.UInt32 or KeyType.UInt64 => $"(({LookupKeyName} as u64) - (Self::MIN_KEY as u64)) as usize",
        _ => $"({LookupKeyName} - Self::MIN_KEY) as usize"
    };
}