using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashTableChainCode<TKey, TValue>(HashTableChainContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() =>
        $$"""
              {{FieldModifier}}{{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{FieldModifier}}E[] _entries = {
          {{FormatColumns(ctx.Entries, x => $"new E({(ctx.StoreHashCode ? $"{x.Hash}, " : "")}{x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} key)
              {
          {{EarlyExits}}

                  {{HashSizeType}} hash = Hash(key);
                  {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                  while (i >= 0)
                  {
                      ref E entry = ref _entries[i];

                      if ({{(ctx.StoreHashCode ? $"{GetEqualFunction("entry.HashCode", "hash", HashSizeDataType)} && " : "")}}{{GetEqualFunction("key", "entry.Key")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{HashSource}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct E
              {
                  {{(ctx.StoreHashCode ? $"internal readonly {HashSizeType} HashCode;" : "")}}
                  internal readonly {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                  internal readonly {{KeyTypeName}} Key;

                  internal E({{(ctx.StoreHashCode ? $"{HashSizeType} hashCode, " : "")}}{{GetSmallestSignedType(ctx.Buckets.Length)}} next, {{KeyTypeName}} key)
                  {
                      {{(ctx.StoreHashCode ? "HashCode = hashCode;" : "")}}
                      Next = next;
                      Key = key;
                  }
              }
          """;
}