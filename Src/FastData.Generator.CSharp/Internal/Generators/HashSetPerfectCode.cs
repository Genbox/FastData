using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}E[] _entries = {
          {{FormatColumns(ctx.Data, x => $"new E({ToValueLabel(x.Key)}, {x.Value.ToStringInvariant()})")}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  {{HashType}} hash = Mixer(Hash(value) ^ {{ctx.Seed}});
                  uint index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                  ref E entry = ref _entries[index];

                  return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
              }

          {{GetHashSource()}}

          {{GetMixer()}}

              [StructLayout(LayoutKind.Auto)]
              private struct E
              {
                  internal E({{TypeName}} value, {{HashType}} hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  internal {{TypeName}} Value;
                  internal {{HashType}} HashCode;
              }
          """;

    private string GetMixer()
    {
        if (GeneratorConfig.Use64BitHashing)
        {
            return """
                       [MethodImpl(MethodImplOptions.AggressiveInlining)]
                       private static ulong Mixer(ulong h)
                       {
                           h ^= h >> 33;
                           h *= 0xFF51AFD7ED558CCD;
                           h ^= h >> 33;
                           h *= 0xC4CEB9FE1A85EC53;
                           h ^= h >> 33;
                           return h;
                       }
                   """;
        }

        return """
                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   private static uint Mixer(uint h)
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
               """;
    }
}