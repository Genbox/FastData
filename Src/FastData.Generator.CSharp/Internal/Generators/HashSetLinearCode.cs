using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetLinearCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, HashSetLinearContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly Bucket[] _buckets = {
          {{JoinValues(ctx.Buckets, RenderBucket, ",\n")}}
              };

              private{{cfg.GetModifier()}} readonly {{genCfg.DataType}}[] _items = {
          {{JoinValues(ctx.Data, RenderItem, ",\n")}}
              };

              private{{cfg.GetModifier()}} readonly uint[] _hashCodes = { {{JoinValues(ctx.HashCodes, RenderHashCode)}} };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint hashCode = Hash(value);
                  ref Bucket b = ref _buckets[{{cfg.GetModFunction("hashCode", (uint)ctx.Buckets.Length)}}];

                  {{GetIndexType(ctx.Data.Length)}} index = b.StartIndex;
                  {{GetIndexType(ctx.Data.Length)}} endIndex = b.EndIndex;

                  while (index <= endIndex)
                  {
                      if (hashCode == _hashCodes[index] && {{genCfg.GetEqualFunction("value", "_items[index]")}})
                          return true;

                      index++;
                  }

                  return false;
              }

          {{genCfg.GetHashSource(false)}}

              [StructLayout(LayoutKind.Auto)]
              private struct Bucket
              {
                  internal Bucket({{GetIndexType(ctx.Data.Length)}} startIndex, {{GetIndexType(ctx.Data.Length)}} endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }

                  internal {{GetIndexType(ctx.Data.Length)}} StartIndex;
                  internal {{GetIndexType(ctx.Data.Length)}} EndIndex;
              }
          """;

    private static string GetIndexType(int itemCount) => itemCount switch
    {
        <= byte.MaxValue => "byte",
        <= ushort.MaxValue => "ushort",
        _ => "uint"
    };

    private static void RenderBucket(StringBuilder sb, Bucket obj) => sb.Append("        new Bucket(").Append(obj.StartIndex).Append(", ").Append(obj.EndIndex).Append(')');
    private static void RenderItem(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    private static void RenderHashCode(StringBuilder sb, uint obj) => sb.Append(obj);
}