using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, HashSetChainContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{GetIndexType(ctx.Data.Length)}}[] _buckets = { {{JoinValues(ctx.Buckets, RenderBucket)}} };

              private{{cfg.GetModifier()}} readonly Entry[] _entries = {
          {{JoinValues(ctx.Entries, RenderEntry, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint hashCode = Hash(value);
                  uint index = {{cfg.GetModFunction("hashCode", (uint)ctx.Buckets.Length)}};
                  {{GetIndexType(ctx.Data.Length)}} i = ({{GetIndexType(ctx.Data.Length)}})(_buckets[index] - 1);

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
                  public {{GetIndexType(ctx.Data.Length)}} Next;
                  public {{genCfg.DataType}} Value;

                  public Entry(uint hashCode, {{GetIndexType(ctx.Data.Length)}} next, {{genCfg.DataType}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;

    private static string GetIndexType(int itemCount) => itemCount switch
    {
        // We have to use signed types because -1 means unmapped
        <= sbyte.MaxValue => "sbyte",
        <= short.MaxValue => "short",
        _ => "int"
    };

    private static void RenderBucket(StringBuilder sb, int obj) => sb.Append(obj);
    private static void RenderEntry(StringBuilder sb, HashSetEntry obj) => sb.Append("        new Entry(").Append(obj.Hash).Append(", ").Append(obj.Next).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
}