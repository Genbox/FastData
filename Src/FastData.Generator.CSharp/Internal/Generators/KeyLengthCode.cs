using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<T>(GeneratorConfig<T> genCfg, CSharpCodeGeneratorConfig cfg, KeyLengthContext ctx) : IOutputWriter
{
    //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

    public string Generate()
    {
        if (ctx.LengthsAreUniq)
        {
            //There is an assumptions, that when LengthsAreUniq is true, all the length buckets only contain one item
            return cfg.KeyLengthUniqBranchType switch
            {
                BranchType.If => GenerateUniqIf(),
                BranchType.Switch => GenerateUniqSwitch(),
                _ => throw new InvalidOperationException("Invalid branch type: " + cfg.KeyLengthUniqBranchType)
            };
        }

        return GenerateNormal();
    }

    private string GenerateUniqIf() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{genCfg.GetTypeName()}}[] _entries = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()), ToValueLabel)}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}

                  return {{genCfg.GetEqualFunction($"_entries[value.Length - {ctx.MinLength.ToStringInvariant()}]")}};
              }
          """;

    private string GenerateUniqSwitch() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  switch (value.Length)
                  {
          {{FormatList(ctx.Lengths.Where(x => x != null).Select(x => x![0]), x => $"""
                                                                                               case {x.Length}:
                                                                                                   return {genCfg.GetEqualFunction(ToValueLabel(x))};
                                                                                   """)}}
                      default:
                          return false;
                  }
              }
          """;

    private string GenerateNormal() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{genCfg.GetTypeName()}}[]?[] _entries = new {{genCfg.GetTypeName()}}[]?[] {
          {{FormatList(ctx.Lengths.Skip((int)ctx.MinLength).Take((int)((ctx.MaxLength - ctx.MinLength) + 1)), RenderMany, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}
                  {{genCfg.GetTypeName()}}[]? bucket = _entries[value.Length - {{ctx.MinLength}}];

                  if (bucket == null)
                      return false;

                  foreach ({{genCfg.GetTypeName()}} str in bucket)
                  {
                      if ({{genCfg.GetEqualFunction("str")}})
                          return true;
                  }

                  return false;
              }
          """;

    private string GetEarlyExit(IEarlyExit[] exits)
    {
        //We do this to force an early exit for this data structure. Otherwise, we will get an IndexOutOfRangeException
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return CSharpGeneratorConfigExtensions.GetLengthEarlyExits(exit1.MinValue, exit1.MaxValue);

        MinMaxValueEarlyExit<T>? exit2 = (MinMaxValueEarlyExit<T>?)Array.Find(exits, x => x is MinMaxValueEarlyExit<T>);
        if (exit2 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits(exit2.MinValue, exit2.MaxValue, genCfg.DataType);

        LengthBitSetEarlyExit? exit3 = (LengthBitSetEarlyExit?)Array.Find(exits, x => x is LengthBitSetEarlyExit);
        if (exit3 != null)
            return CSharpGeneratorConfigExtensions.GetMaskEarlyExit(exit3.BitSet);

        throw new InvalidOperationException("No early exits were found. They are required for UniqueKeyLength");
    }

    private static string RenderMany(List<string>? x)
    {
        if (x == null)
            return "        null";

        return $"new [] {{{string.Join(",", x.Select(ToValueLabel))}}}";
    }
}