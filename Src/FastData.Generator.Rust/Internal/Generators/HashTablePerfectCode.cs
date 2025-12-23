using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashTablePerfectCode<TKey, TValue>(HashTablePerfectContext<TKey, TValue> ctx, GeneratorConfig<TKey> genCfg, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        ReadOnlyMemory<TValue> values = ctx.Values;

        StringBuilder sb = new StringBuilder();
        string typeName;
        Func<int, KeyValuePair<TKey, ulong>, string> printer;

        //If we need to store additional values (hash codes or values), we need a struct. Else we just use the key directly
        if (ctx.StoreHashCode || !ctx.Values.IsEmpty)
        {
            typeName = "e";
            printer = (i, x) => $"e::new({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Value.ToStringInvariant()}" : "")}{(!ctx.Values.IsEmpty ? $", {ToValueLabel(values.Span[i])}" : "")})";

            shared.Add(CodePlacement.Before, $$"""
                                               pub struct e {
                                                   pub key: {{GetKeyTypeName(customKey)}},
                                                   {{(ctx.StoreHashCode ? $"pub hash_code: {HashSizeType}," : "")}}
                                                   {{(!ctx.Values.IsEmpty ? $"pub value: {GetValueTypeName(customValue)}," : "")}}
                                               }
                                               """);

            shared.Add(CodePlacement.Before, $$"""
                                               impl e {
                                                   pub const fn new (key: {{KeyTypeName}}{{(ctx.StoreHashCode ? $", hash_code: {HashSizeType}" : "")}}{{(!ctx.Values.IsEmpty ? $", value: {GetValueTypeName(customValue)}" : "")}}) -> Self { Self { key{{(ctx.StoreHashCode ? ", hash_code" : "")}}{{(!ctx.Values.IsEmpty ? ", value" : "")}}  } }
                                               }
                                               """);
        }
        else
        {
            typeName = KeyTypeName;
            printer = (_, x) => ToValueLabel(x.Key);
        }

        sb.Append($$"""

                    {{FieldModifier}}ENTRIES: [{{typeName}}; {{ctx.Data.Length.ToStringInvariant()}}] = [
                        {{FormatColumns(ctx.Data, printer)}}
                    ];

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            let hash = unsafe { Self::get_hash({{LookupKeyName}}) };
                            let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;

                            return {{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "&Self::ENTRIES[index].hash_code", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, ctx.StoreHashCode || !ctx.Values.IsEmpty ? "Self::ENTRIES[index].key" : "Self::ENTRIES[index]")}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{KeyTypeName}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                let hash = unsafe { Self::get_hash({{LookupKeyName}}) };
                                let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;
                                let entry = &Self::ENTRIES[index];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.hash_code", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, "entry.key")}}) {
                                    return Some(entry.value);
                                }

                                None
                            }
                        """);
        }

        return sb.ToString();
    }
}