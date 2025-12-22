using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashTablePerfectCode<TKey, TValue>(HashTablePerfectContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();
        string typeName;
        Func<int, KeyValuePair<TKey, ulong>, string> printer;

        //If we need to store additional values (hash codes or values), we need a struct. Else we just use the key directly
        if (ctx.StoreHashCode || ctx.Values != null)
        {
            typeName = "E";
            printer = (i, x) => $"new E({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Value.ToStringInvariant()}" : "")}{(ctx.Values != null ? $", {ToValueLabel(ctx.Values[i])}" : "")})";

            sb.Append($$"""
                        [StructLayout(LayoutKind.Auto)]
                        private struct E
                        {
                            internal {{KeyTypeName}} Key;
                            {{(ctx.StoreHashCode ? $"internal {HashSizeType} HashCode;" : "")}}
                            {{(ctx.Values != null ? $"internal {ValueTypeName} Value;" : "")}}

                            internal E({{KeyTypeName}} key{{(ctx.StoreHashCode ? $", {HashSizeType} hashCode" : "")}}{{(ctx.Values != null ? $", {ValueTypeName} value" : "")}})
                            {
                                Key = key;
                                {{(ctx.StoreHashCode ? "HashCode = hashCode;" : "")}}
                                {{(ctx.Values != null ? "Value = value;" : "")}}
                            }
                        }
                        """);
        }
        else
        {
            typeName = KeyTypeName;
            printer = (_, x) => ToValueLabel(x.Key);
        }

        sb.Append($$"""

                        {{FieldModifier}}{{typeName}}[] _entries = {
                    {{FormatColumns(ctx.Data, printer)}}
                        };

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            {{HashSizeType}} hash = Hash({{LookupKeyName}});
                            {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                            ref var entry = ref _entries[index];

                            return {{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.HashCode", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, ctx.StoreHashCode || ctx.Values != null ? "entry.Key" : "entry")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}} value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                {{HashSizeType}} hash = Hash({{LookupKeyName}});
                                {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                                ref E entry = ref _entries[index];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("hash", "entry.HashCode", KeyType.Int64)} && " : "")}}{{GetEqualFunction(LookupKeyName, "entry.Key")}})
                                {
                                    value = entry.Value;
                                    return true;
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}