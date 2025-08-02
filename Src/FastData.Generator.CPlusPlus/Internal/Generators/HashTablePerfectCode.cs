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

        //If we need to store additional values (hash codes or values), we need a struct. Else we just use the key directly
        if (ctx.StoreHashCode || ctx.Values != null)
        {
            typeName = "e";
            printer = (i, x) => $"e({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Value.ToStringInvariant()}" : "")}{(ctx.Values != null ? $", {ToValueLabel(ctx.Values[i])}" : "")})";

            sb.Append($$"""
                            struct e {
                                {{KeyTypeName}} key;
                                {{(ctx.StoreHashCode ? $"{HashSizeType} hash_code;" : "")}}
                                {{(ctx.Values != null ? $"const {GetValueTypeName(customValue)} value;" : "")}}

                                constexpr e(const {{KeyTypeName}} key{{(ctx.StoreHashCode ? $", const {HashSizeType} hash_code" : "")}}{{(ctx.Values != null ? $", const {GetValueTypeName(customValue)} value" : "")}}){{PostMethodModifier}}
                                : key(key){{(ctx.StoreHashCode ? ", hash_code(hash_code)" : "")}}{{(ctx.Values != null ? ", value(value)" : "")}} {}
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
                    {{GetEarlyExits(MethodType.Contains)}}

                            const {{HashSizeType}} hash = get_hash(key);
                            const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                            const auto& entry = entries[index];

                            return {{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.hash_code")} && " : "")}}{{GetEqualFunction("key", ctx.StoreHashCode || ctx.Values != null ? "entry.key" : "entry")}};
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
                                const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                                const auto& entry = entries[index];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.hash_code")} && " : "")}}{{GetEqualFunction("key", "entry.key")}}) {
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