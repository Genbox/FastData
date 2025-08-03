using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashTableCode<TKey, TValue>(HashTableContext<TKey, TValue> ctx, GeneratorConfig<TKey> genCfg, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;

        shared.Add(CodePlacement.After, $$"""
                                          struct E {
                                              {{(ctx.StoreHashCode ? $"hash_code: {HashSizeType}," : "")}}
                                              next: {{GetSmallestSignedType(ctx.Buckets.Length)}},
                                              key: {{GetKeyTypeName(customKey)}},
                                              {{(ctx.Values != null ? $"value: {GetValueTypeName(customValue)}," : "")}}
                                          }
                                          """);

        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}BUCKETS: [{{GetSmallestSignedType(ctx.Buckets.Length)}}; {{ctx.Buckets.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Buckets, (_, x) => x.ToStringInvariant())}}
                        ];

                        {{FieldModifier}}ENTRIES: [E; {{ctx.Entries.Length}}] = [
                    {{FormatColumns(ctx.Entries, (i, x) => $"E {{ {(ctx.StoreHashCode ? $"hash_code: {x.Hash}, " : "")}next: {x.Next.ToStringInvariant()}, key: {ToValueLabel(x.Key)}{(ctx.Values != null ? $", value: {ToValueLabel(ctx.Values[i])}" : "")} }}")}}
                        ];

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetEarlyExits(MethodType.Contains)}}

                            let hash = unsafe { Self::get_hash(key) };
                            let index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                            let mut i: {{GetSmallestSignedType(ctx.Buckets.Length)}} = (Self::BUCKETS[index as usize] as {{GetSmallestSignedType(ctx.Buckets.Length)}}) - 1;

                            while i >= 0 {
                                let entry = &Self::ENTRIES[i as usize];
                                if {{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", "key")}} {
                                    return true;
                                }
                                i = entry.next;
                            }

                            false
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                let hash = unsafe { Self::get_hash(key) };
                                let index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                                let mut i: {{GetSmallestSignedType(ctx.Buckets.Length)}} = (Self::BUCKETS[index as usize] as {{GetSmallestSignedType(ctx.Buckets.Length)}}) - 1;

                                while i >= 0 {
                                    let entry = &Self::ENTRIES[i as usize];
                                    if {{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", "key")}} {
                                        return Some(entry.value);
                                    }
                                    i = entry.next;
                                }

                                None
                            }
                        """);
        }

        return sb.ToString();
    }
}