using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode<T>(HashSetChainContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}{{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
          {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
               };

              {{FieldModifier}}E[] _entries = {
          {{FormatColumns(ctx.Entries, x => $"new E({x.Hash}, {x.Next.ToStringInvariant()}, {ToValueLabel(x.Value)})")}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  {{HashSizeType}} hash = Hash(value);
                  {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                  while (i >= 0)
                  {
                      ref E entry = ref _entries[i];

                      if ({{GetEqualFunction("entry.HashCode", "hash", HashSizeDataType)}} && {{GetEqualFunction("value", "entry.Value")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{HashSource}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct E
              {
                  internal readonly {{HashSizeType}} HashCode;
                  internal readonly {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                  internal readonly {{TypeName}} Value;

                  internal E({{HashSizeType}} hashCode, {{GetSmallestSignedType(ctx.Buckets.Length)}} next, {{TypeName}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;
}