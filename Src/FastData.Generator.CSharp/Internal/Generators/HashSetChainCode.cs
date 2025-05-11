using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode<T>(HashSetChainContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}{{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{GetFieldModifier()}}E[] _entries = {
          {{FormatColumns(ctx.Entries, x => $"new E({x.Hash}, {x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  uint hash = Hash(value);
                  uint index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                  while (i >= 0)
                  {
                      ref E entry = ref _entries[i];

                      if (entry.HashCode == hash && {{GetEqualFunction("value", "entry.Value")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{GetHashSource()}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct E
              {
                  internal readonly uint HashCode;
                  internal readonly {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                  internal readonly {{TypeName}} Value;

                  internal E(uint hashCode, {{GetSmallestSignedType(ctx.Buckets.Length)}} next, {{TypeName}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;
}