using Genbox.FastData.Generator.CSharp.Internal.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}E[] _entries = {
          {{FormatColumns(ctx.Data, x => $"new E({ToValueLabel(x.Key)}, {ToValueLabel(x.Value)})")}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  uint hash = Murmur_32(Hash(value) ^ {{ctx.Seed}});
                  uint index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                  ref E entry = ref _entries[index];

                  return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
              }

          {{GetHashSource()}}

              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static uint Murmur_32(uint h)
              {
                  unchecked
                  {
                      h ^= h >> 16;
                      h *= 0x85EBCA6BU;
                      h ^= h >> 13;
                      h *= 0xC2B2AE35U;
                      h ^= h >> 16;
                      return h;
                  }
              }

              [StructLayout(LayoutKind.Auto)]
              private struct E
              {
                  internal E({{TypeName}} value, uint hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  internal {{TypeName}} Value;
                  internal uint HashCode;
              }
          """;
}