using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetLinearCode(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, HashSetLinearContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}B[] _buckets = {
          {{FormatColumns(ctx.Buckets, static x => $"new B({x.StartIndex.ToStringInvariant()}, {x.EndIndex.ToStringInvariant()})")}}
              };

              {{cfg.GetFieldModifier()}}{{genCfg.GetTypeName()}}[] _items = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{cfg.GetFieldModifier()}}uint[] _hashCodes = {
          {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
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

          {{genCfg.GetHashSource()}}

              [StructLayout(LayoutKind.Auto)]
              private readonly struct B
              {
                  internal readonly {{GetSmallestUnsignedType(ctx.Data.Length)}} StartIndex;
                  internal readonly {{GetSmallestUnsignedType(ctx.Data.Length)}} EndIndex;

                  internal B({{GetSmallestUnsignedType(ctx.Data.Length)}} startIndex, {{GetSmallestUnsignedType(ctx.Data.Length)}} endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }
              }
          """;
}