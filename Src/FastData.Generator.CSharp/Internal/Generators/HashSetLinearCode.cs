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

                  int index = b.StartIndex;
                  int endIndex = b.EndIndex;

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
                  internal Bucket(int startIndex, int endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }

                  internal int StartIndex;
                  internal int EndIndex;
              }
          """;

    //TODO: Start and End index can be smaller if there are fewer items
    //TODO: Either implement a bitmap for seen buckets everywhere or don't use bitmaps for simplicity

    private static void RenderBucket(StringBuilder sb, Bucket obj) => sb.Append("        new Bucket(").Append(obj.StartIndex).Append(", ").Append(obj.EndIndex).Append(')');
    private static void RenderItem(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    private static void RenderHashCode(StringBuilder sb, uint obj) => sb.Append(obj);
}