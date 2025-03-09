using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, HashSetChainContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly int[] _buckets = { {{JoinValues(ctx.Buckets, RenderBucket)}} };

              private{{cfg.GetModifier()}} readonly Entry[] _entries = {
          {{JoinValues(ctx.Entries, RenderEntry, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint hashCode = Hash(value);
                  uint index = {{cfg.GetModFunction("hashCode", (uint)ctx.Buckets.Length)}};
                  int i = _buckets[index] - 1;

                  while (i >= 0)
                  {
                      ref Entry entry = ref _entries[i];

                      if (entry.HashCode == hashCode && {{genCfg.GetEqualFunction("entry.Value", "value")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{genCfg.GetHashSource(false)}}

              [StructLayout(LayoutKind.Auto)]
              private struct Entry
              {
                  public uint HashCode;
                  public {{(ctx.Data.Length <= short.MaxValue ? "short" : "int")}} Next;
                  public {{genCfg.DataType}} Value;

                  public Entry(uint hashCode, {{(ctx.Data.Length <= short.MaxValue ? "short" : "int")}} next, {{genCfg.DataType}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;

    private static void RenderBucket(StringBuilder sb, int obj) => sb.Append(obj);
    private static void RenderEntry(StringBuilder sb, Entry obj) => sb.Append("        new Entry(").Append(obj.HashCode).Append(", ").Append(obj.Next).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
}