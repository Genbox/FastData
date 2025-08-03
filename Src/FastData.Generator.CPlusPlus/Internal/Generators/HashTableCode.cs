using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashTableCode<TKey, TValue>(HashTableContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        struct e {
                            {{KeyTypeName}} key;
                            {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                            {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                            {{(ctx.Values != null ? $"const {GetValueTypeName(customValue)} value;" : "")}}
                            e(const {{KeyTypeName}} key, const {{GetSmallestSignedType(ctx.Buckets.Length)}} next{{(ctx.StoreHashCode ? $", const {HashSizeType} hash_code" : "")}}{{(ctx.Values != null ? $", const {GetValueTypeName(customValue)} value" : "")}})
                               : key(key), next(next){{(ctx.StoreHashCode ? ", hash_code(hash_code)" : "")}}{{(ctx.Values != null ? ", value(value)" : "")}} {}
                        };

                        {{GetFieldModifier(true)}}std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length.ToStringInvariant()}}> buckets = {
                    {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                         };

                        {{GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length.ToStringInvariant()}}> entries = {
                    {{FormatColumns(ctx.Entries, (i, x) => $"e({ToValueLabel(x.Key)}, {x.Next.ToStringInvariant()}{(ctx.StoreHashCode ? $", {x.Hash.ToStringInvariant()}" : "")}{(ctx.Values != null ? $", {ToValueLabel(ctx.Values[i])}" : "")})")}}
                        };

                    {{HashSource}}

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetEarlyExits(MethodType.Contains)}}

                            const {{HashSizeType}} hash = get_hash(key);
                            const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                            {{GetSmallestSignedType(ctx.Buckets.Length)}} i = static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(buckets[index] - 1);

                            while (i >= 0) {
                                const auto& entry = entries[i];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", "key")}})
                                    return true;

                                i = entry.next;
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
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                const {{HashSizeType}} hash = get_hash(key);
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                                {{GetSmallestSignedType(ctx.Buckets.Length)}} i = static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(buckets[index] - 1);

                                while (i >= 0) {
                                    const auto& entry = entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.hash_code", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.key", "key")}}) {
                                        value = {{ptr}}entry.value;
                                        return true;
                                    }

                                    i = entry.next;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}