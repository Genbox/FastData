using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, HashSetChainContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
          {{FormatColumns(ctx.Buckets, static (sb, x) => sb.Append(x))}}
               };

              private{{cfg.GetModifier()}} readonly E[] _entries = {
          {{FormatColumns(ctx.Entries, RenderEntry)}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  uint hash = Hash(value);
                  uint index = {{cfg.GetModFunction(ctx.Buckets.Length)}};
                  {{GetSmallestSignedType(ctx.Buckets.Length)}} i = ({{GetSmallestSignedType(ctx.Buckets.Length)}})(_buckets[index] - 1);

                  while (i >= 0)
                  {
                      ref E entry = ref _entries[i];

                      if (entry.HashCode == hash && {{genCfg.GetEqualFunction("entry.Value")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{genCfg.GetHashSource(false)}}

              [StructLayout(LayoutKind.Auto)]
              private struct E
              {
                  internal uint HashCode;
                  internal {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                  internal {{genCfg.GetTypeName()}} Value;

                  internal E(uint hashCode, {{GetSmallestSignedType(ctx.Buckets.Length)}} next, {{genCfg.GetTypeName()}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;

    private static void RenderEntry(StringBuilder sb, HashSetEntry x) => sb.Append("new E(").Append(x.Hash).Append(", ").Append(x.Next).Append(", ").Append(ToValueLabel(x.Value)).Append(')');
}