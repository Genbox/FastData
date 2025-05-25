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

                  ulong hash = Hash(value);
                  uint index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                  ref E entry = ref _entries[index];

                  return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
              }

          {{GetHashSource()}}

              [StructLayout(LayoutKind.Auto)]
              private struct E
              {
                  internal E({{TypeName}} value, ulong hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  internal {{TypeName}} Value;
                  internal ulong HashCode;
              }
          """;
}