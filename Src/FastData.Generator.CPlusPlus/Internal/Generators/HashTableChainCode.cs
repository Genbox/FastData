using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashTableChainCode<TKey, TValue>(HashTableChainContext<TKey, TValue> ctx, SharedCode shared, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($$"""
                            struct e
                            {
                                {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                                {{GetSmallestSignedType(ctx.Buckets.Length)}} next;
                                {{KeyTypeName}} key;

                                e({{(ctx.StoreHashCode ? $"const {HashSizeType} hash_code, " : "")}}const {{GetSmallestSignedType(ctx.Buckets.Length)}} next, const {{KeyTypeName}} key)
                                   : {{(ctx.StoreHashCode ? "hash_code(hash_code), " : "")}}next(next), key(key) {}
                            };

                            {{FieldModifier}}std::array<{{GetSmallestSignedType(ctx.Buckets.Length)}}, {{ctx.Buckets.Length.ToStringInvariant()}}> buckets = {
                        {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                             };

                            {{GetFieldModifier(false)}}std::array<e, {{ctx.Entries.Length.ToStringInvariant()}}> entries = {
                        {{FormatColumns(ctx.Entries, x => $"e({(ctx.StoreHashCode ? $"{x.Hash.ToStringInvariant()}, " : "")}{x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
                            };

                        {{HashSource}}

                        public:
                            {{MethodAttribute}}
                            {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                const {{HashSizeType}} hash = get_hash(key);
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                                {{GetSmallestSignedType(ctx.Buckets.Length)}} i = buckets[index] - static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(1);

                                while (i >= 0)
                                {
                                    const auto& [{{(ctx.StoreHashCode ? "hash_code, " : "")}}next, key1] = entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash_code", "hash")} && " : "")}}{{GetEqualFunction("key1", "key")}})
                                        return true;

                                    i = next;
                                }

                                return false;
                            }
                        """);

        if (ctx.Values != null && ObjectType != null)
        {
            shared.Add("values", CodePlacement.After, $$"""
                                                        std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> {{className}}::values = {
                                                        {{ValueString}}
                                                        };
                                                        """);

            if (ObjectType.IsCustomType)
                shared.Add("classes", CodePlacement.Before, GetObjectDeclarations(ObjectType));

            sb.Append($$"""
                            static std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> values;

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                const {{HashSizeType}} hash = get_hash(key);
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                                {{GetSmallestSignedType(ctx.Buckets.Length)}} i = buckets[index] - static_cast<{{GetSmallestSignedType(ctx.Buckets.Length)}}>(1);

                                while (i >= 0)
                                {
                                    const auto& [{{(ctx.StoreHashCode ? "hash_code, " : "")}}next, key1] = entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash_code", "hash")} && " : "")}}{{GetEqualFunction("key1", "key")}})
                                        return true;

                                    i = next;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}