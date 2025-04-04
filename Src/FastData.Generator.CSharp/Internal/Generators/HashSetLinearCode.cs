using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetLinearCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, HashSetLinearContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly B[] _buckets = {
          {{FormatColumns(ctx.Buckets, RenderBucket)}}
              };

              private{{cfg.GetModifier()}} readonly {{genCfg.GetTypeName()}}[] _items = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              private{{cfg.GetModifier()}} readonly uint[] _hashCodes = {
          {{FormatColumns(ctx.HashCodes, static (sb, obj) => sb.Append(obj))}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint hashCode = Hash(value);
                  ref B b = ref _buckets[{{cfg.GetModFunction("hashCode", (uint)ctx.Buckets.Length)}}];

                  {{GetSmallestUnsignedType(ctx.Data.Length)}} index = b.StartIndex;
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex = b.EndIndex;

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
              private struct B
              {
                  internal B({{GetSmallestUnsignedType(ctx.Data.Length)}} startIndex, {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }

                  internal {{GetSmallestUnsignedType(ctx.Data.Length)}} StartIndex;
                  internal {{GetSmallestUnsignedType(ctx.Data.Length)}} EndIndex;
              }
          """;

    private static void RenderBucket(StringBuilder sb, HashSetBucket x) => sb.Append("new B(").Append(x.StartIndex).Append(", ").Append(x.EndIndex).Append(')');
}