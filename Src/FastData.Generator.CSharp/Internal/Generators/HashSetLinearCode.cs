using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;

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
          {{cfg.GetEarlyExits(genCfg)}}
          
                  uint hash = Hash(value);
                  ref B b = ref _buckets[{{cfg.GetModFunction(ctx.Buckets.Length)}}];
          
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} index = b.StartIndex;
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex = b.EndIndex;
          
                  while (index <= endIndex)
                  {
                      if (_hashCodes[index] == hash && {{genCfg.GetEqualFunction("_items[index]")}})
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