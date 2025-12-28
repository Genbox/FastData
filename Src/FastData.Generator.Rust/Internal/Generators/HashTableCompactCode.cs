using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashTableCompactCode<TKey, TValue>(HashTableCompactContext<TKey, TValue> ctx, GeneratorConfig<TKey> genCfg, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        ReadOnlyMemory<TValue> values = ctx.Values;

        shared.Add(CodePlacement.After, $$"""
                                          struct E {
                                              {{(ctx.StoreHashCode ? $"hash_code: {HashSizeType}," : "")}}
                                              key: {{GetKeyTypeName(customKey)}},
                                              {{(!values.IsEmpty ? $"value: {GetValueTypeName(customValue)}," : "")}}
                                          }
                                          """);

        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}BUCKET_STARTS: [{{GetSmallestUnsignedType(ctx.Entries.Length)}}; {{ctx.BucketStarts.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.BucketStarts, (_, x) => x.ToStringInvariant())}}
                        ];

                        {{FieldModifier}}BUCKET_COUNTS: [{{GetSmallestUnsignedType(ctx.Entries.Length)}}; {{ctx.BucketCounts.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.BucketCounts, (_, x) => x.ToStringInvariant())}}
                        ];

                        {{FieldModifier}}ENTRIES: [E; {{ctx.Entries.Length}}] = [
                    {{FormatColumns(ctx.Entries, (i, x) => $"E {{ {(ctx.StoreHashCode ? $"hash_code: {x.Hash}, " : "")}key: {ToValueLabel(x.Key)}{(!ctx.Values.IsEmpty ? $", value: {ToValueLabel(values.Span[i])}" : "")} }}")}}
                        ];

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            let hash = unsafe { Self::get_hash({{LookupKeyName}}) };
                            let index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                            let start = Self::BUCKET_STARTS[index as usize] as usize;
                            let count = Self::BUCKET_COUNTS[index as usize] as usize;
                            let end = start + count;

                            for i in start..end {
                                let entry = &Self::ENTRIES[i];
                                if {{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", LookupKeyName)}} {
                                    return true;
                                }
                            }

                            false
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                let hash = unsafe { Self::get_hash({{LookupKeyName}}) };
                                let index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                                let start = Self::BUCKET_STARTS[index as usize] as usize;
                                let count = Self::BUCKET_COUNTS[index as usize] as usize;
                                let end = start + count;

                                for i in start..end {
                                    let entry = &Self::ENTRIES[i];
                                    if {{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", LookupKeyName)}} {
                                        return Some(entry.value);
                                    }
                                }

                                None
                            }
                        """);
        }

        return sb.ToString();
    }
}