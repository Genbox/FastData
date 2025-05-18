using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetLinearCode<T>(HashSetLinearContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}B[] _buckets = {
          {{FormatColumns(ctx.Buckets, static x => $"new B({x.StartIndex.ToStringInvariant()}, {x.EndIndex.ToStringInvariant()})")}}
              };

              {{GetFieldModifier()}}{{TypeName}}[] _items = new {{TypeName}}[] {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{GetFieldModifier()}}ulong[] _hashCodes = {
          {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  ulong hash = Hash(value);
                  ref B b = ref _buckets[{{GetModFunction("hash", (ulong)ctx.Buckets.Length)}}];

                  {{GetSmallestUnsignedType(ctx.Data.Length)}} index = b.StartIndex;
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex = b.EndIndex;

                  while (index <= endIndex)
                  {
                      if (_hashCodes[index] == hash && {{GetEqualFunction("value", "_items[index]")}})
                          return true;

                      index++;
                  }

                  return false;
              }

          {{GetHashSource()}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct B
              {
                  internal readonly {{GetSmallestUnsignedType(ctx.Data.Length)}} StartIndex;
                  internal readonly {{GetSmallestUnsignedType(ctx.Data.Length)}} EndIndex;

                  internal B({{GetSmallestUnsignedType(ctx.Data.Length)}} startIndex, {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }
              }
          """;
}