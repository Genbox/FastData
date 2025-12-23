using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashTablePerfectCode<TKey, TValue>(HashTablePerfectContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();
        string typeName;
        Func<int, KeyValuePair<TKey, ulong>, string> printer;
        ReadOnlyMemory<TValue> values = ctx.Values;

        //If we need to store additional values (hash codes or values), we need a struct. Else we just use the key directly
        if (ctx.StoreHashCode || !ctx.Values.IsEmpty)
        {
            typeName = "e";
            printer = (i, x) => $"e({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Value.ToStringInvariant()}" : "")}{(!ctx.Values.IsEmpty ? $", {ToValueLabel(values.Span[i])}" : "")})";

            sb.Append($$"""
                            struct e {
                                {{KeyTypeName}} key;
                                {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                                {{(!ctx.Values.IsEmpty ? $"const {GetValueTypeName(customValue)} value;" : "")}}

                                constexpr e(const {{KeyTypeName}} key{{(ctx.StoreHashCode ? $", const {HashSizeType} hash_code" : "")}}{{(!ctx.Values.IsEmpty ? $", const {GetValueTypeName(customValue)} value" : "")}}){{PostMethodModifier}}
                                : key(key){{(ctx.StoreHashCode ? ", hash_code(hash_code)" : "")}}{{(!ctx.Values.IsEmpty ? ", value(value)" : "")}} {}
                            };
                        """);
        }
        else
        {
            typeName = KeyTypeName;
            printer = (_, x) => ToValueLabel(x.Key);
        }

        sb.Append($$"""

                        {{GetFieldModifier(false)}}std::array<{{typeName}}, {{ctx.Data.Length.ToStringInvariant()}}> entries = {
                    {{FormatColumns(ctx.Data, printer)}}
                        };

                    {{HashSource}}

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            const {{HashSizeType}} hash = get_hash({{LookupKeyName}});
                            const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                            const auto& entry = entries[index];

                            return {{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.hash_code", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, ctx.StoreHashCode || !ctx.Values.IsEmpty ? "entry.key" : "entry")}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                const {{HashSizeType}} hash = get_hash({{LookupKeyName}});
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                                const auto& entry = entries[index];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.hash_code", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, "entry.key")}}) {
                                    value = {{ptr}}entry.value;
                                    return true;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}