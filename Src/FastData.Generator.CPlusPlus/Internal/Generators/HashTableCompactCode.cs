using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashTableCompactCode<TKey, TValue>(HashTableCompactContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        struct e {
                            {{KeyTypeName}} key;
                            {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                            {{(ctx.Values != null ? $"const {GetValueTypeName(customValue)} value;" : "")}}
                            e(const {{KeyTypeName}} key{{(ctx.StoreHashCode ? $", const {HashSizeType} hash_code" : "")}}{{(ctx.Values != null ? $", const {GetValueTypeName(customValue)} value" : "")}})
                               : key(key){{(ctx.StoreHashCode ? ", hash_code(hash_code)" : "")}}{{(ctx.Values != null ? ", value(value)" : "")}} {}
                        };

                        {{GetFieldModifier(true)}}std::array<{{GetSmallestUnsignedType(ctx.Entries.Length)}}, {{ctx.BucketStarts.Length.ToStringInvariant()}}> bucket_starts = {
                    {{FormatColumns(ctx.BucketStarts, static x => x.ToStringInvariant())}}
                         };

                        {{GetFieldModifier(true)}}std::array<{{GetSmallestUnsignedType(ctx.Entries.Length)}}, {{ctx.BucketCounts.Length.ToStringInvariant()}}> bucket_counts = {
                    {{FormatColumns(ctx.BucketCounts, static x => x.ToStringInvariant())}}
                         };

                        {{GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length.ToStringInvariant()}}> entries = {
                    {{FormatColumns(ctx.Entries, (i, x) => $"e({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Hash.ToStringInvariant()}" : "")}{(ctx.Values != null ? $", {ToValueLabel(ctx.Values[i])}" : "")})")}}
                        };

                    {{HashSource}}

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            const {{HashSizeType}} hash = get_hash({{LookupKeyName}});
                            const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                            const size_t start = static_cast<size_t>(bucket_starts[index]);
                            const size_t count = static_cast<size_t>(bucket_counts[index]);
                            const size_t end = start + count;

                            for (size_t i = start; i < end; i++) {
                                const auto& entry = entries[i];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", LookupKeyName)}})
                                    return true;
                            }

                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                const {{HashSizeType}} hash = get_hash({{LookupKeyName}});
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                                const size_t start = static_cast<size_t>(bucket_starts[index]);
                                const size_t count = static_cast<size_t>(bucket_counts[index]);
                                const size_t end = start + count;

                                for (size_t i = start; i < end; i++) {
                                    const auto& entry = entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", LookupKeyName)}}) {
                                        value = {{ptr}}entry.value;
                                        return true;
                                    }
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}
