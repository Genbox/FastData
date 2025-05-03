using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, HashSetChainContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{GetSmallestSignedType(ctx.Buckets.Length)}}[] _buckets = new {{GetSmallestSignedType(ctx.Buckets.Length)}}[] {
          {{FormatColumns(ctx.Buckets, static (sb, x) => sb.Append(x))}}
               };

              {{cfg.GetFieldModifier()}}E[] _entries = {
          {{FormatColumns(ctx.Entries, RenderEntry)}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
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

          {{genCfg.GetHashSource()}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct E
              {
                  internal readonly uint HashCode;
                  internal readonly {{GetSmallestSignedType(ctx.Buckets.Length)}} Next;
                  internal readonly {{genCfg.GetTypeName()}} Value;

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