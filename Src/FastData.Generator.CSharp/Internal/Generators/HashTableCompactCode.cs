using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashTableCompactCode<TKey, TValue>(HashTableCompactContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
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
                            {{(ctx.StoreHashCode ? $"internal {HashSizeType} HashCode;" : "")}}
                            {{(!values.IsEmpty ? $"internal {ValueTypeName} Value;" : "")}}
                            internal E({{KeyTypeName}} key{{(ctx.StoreHashCode ? $", {HashSizeType} hashCode" : "")}}{{(!values.IsEmpty ? $", {ValueTypeName} value" : "")}})
                            {
                                Key = key;
                                {{(ctx.StoreHashCode ? "HashCode = hashCode;" : "")}}
                                {{(!values.IsEmpty ? "Value = value;" : "")}}
                            }
                        };

                        {{FieldModifier}}{{GetSmallestUnsignedType(ctx.Entries.Length)}}[] _bucketStarts = new {{GetSmallestUnsignedType(ctx.Entries.Length)}}[] {
                    {{FormatColumns(ctx.BucketStarts, static x => x.ToStringInvariant())}}
                         };

                        {{FieldModifier}}{{GetSmallestUnsignedType(ctx.Entries.Length)}}[] _bucketCounts = new {{GetSmallestUnsignedType(ctx.Entries.Length)}}[] {
                    {{FormatColumns(ctx.BucketCounts, static x => x.ToStringInvariant())}}
                         };

                        {{FieldModifier}}E[] _entries = {
                    {{FormatColumns(ctx.Entries, (i, x) => $"new E({ToValueLabel(x.Key)}{(ctx.StoreHashCode ? $", {x.Hash.ToStringInvariant()}" : "")}{(!ctx.Values.IsEmpty ? $", {ToValueLabel(values.Span[i])}" : "")})")}}
                        };

                    {{HashSource}}

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            {{HashSizeType}} hash = Hash({{LookupKeyName}});
                            {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                            int start = (int)_bucketStarts[index];
                            int count = (int)_bucketCounts[index];
                            int end = start + count;

                            for (int i = start; i < end; i++)
                            {
                                ref E entry = ref _entries[i];

                                if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.HashCode", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.Key", LookupKeyName)}})
                                    return true;
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
                                {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.BucketStarts.Length)}};
                                int start = (int)_bucketStarts[index];
                                int count = (int)_bucketCounts[index];
                                int end = start + count;

                                for (int i = start; i < end; i++)
                                {
                                    ref E entry = ref _entries[i];

                                    if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.HashCode", "hash", KeyType.Int64)} && " : "")}}{{GetEqualFunction("entry.Key", LookupKeyName)}})
                                    {
                                        value = entry.Value;
                                        return true;
                                    }
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}