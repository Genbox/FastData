using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashTableCode<TKey, TValue>(HashTableContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();
        ReadOnlyMemory<TValue> values = ctx.Values;

        sb.Append($$"""
                        [StructLayout(LayoutKind.Auto)]
                        private struct E
                        {
                            internal {{KeyTypeName}} Key;
                            internal {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                            {{(ctx.StoreHashCode ? $"internal {HashSizeType} HashCode;" : "")}}
                            {{(!values.IsEmpty ? $"internal {ValueTypeName} Value;" : "")}}
                            internal E({{KeyTypeName}} key, {{GetSmallestSignedType(ctx.Buckets.Length)}} next{{(ctx.StoreHashCode ? $", {HashSizeType} hashCode" : "")}} {{(!ctx.Values.IsEmpty ? $", {ValueTypeName} value" : "")}})
                            {
                                Key = key;
                                Next = next;
                                {{(ctx.StoreHashCode ? "HashCode = hashCode;" : "")}}
                                {{(!ctx.Values.IsEmpty ? "Value = value;" : "")}}
                            }
                        };

                        {{FieldModifier}}{{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
                    {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                         };

                        {{FieldModifier}}E[] _entries = {
                    {{FormatColumns(ctx.Entries, (i, x) => $"new E({ToValueLabel(x.Key)}, {x.Next.ToStringInvariant()}{(ctx.StoreHashCode ? $", {x.Hash.ToStringInvariant()}" : "")}{(!ctx.Values.IsEmpty ? $", {ToValueLabel(values.Span[i])}" : "")})")}}
                        };

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            {{HashSizeType}} hash = Hash({{LookupKeyName}});
                            {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                            {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                            while (i >= 0)
                            {
                                ref E entry = ref _entries[i];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.HashCode", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.Key", LookupKeyName)}})
                                    return true;

                                i = entry.Next;
                            }

                            return false;
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}} value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                {{HashSizeType}} hash = Hash({{LookupKeyName}});
                                {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                                {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                                while (i >= 0)
                                {
                                    ref E entry = ref _entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.HashCode", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.Key", LookupKeyName)}})
                                    {
                                        value = entry.Value;
                                        return true;
                                    }

                                    i = entry.Next;
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}