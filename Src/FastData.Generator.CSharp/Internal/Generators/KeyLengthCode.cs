using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, KeyLengthContext ctx) : IOutputWriter
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
                _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
            };
        }

        return GenerateNormal();
    }

    private string GenerateUniqIf() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{genCfg.DataType}}[] _entries = new {{genCfg.DataType}}[] {
          {{JoinValues(ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()), RenderOne, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}

                  return {{genCfg.GetEqualFunction("value", $"_entries[value.Length - {ctx.MinLength.ToString(NumberFormatInfo.InvariantInfo)}]")}};
              }
          """;

    private string GenerateUniqSwitch() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  switch (value.Length)
                  {
          {{GenerateSwitch(genCfg, ctx.Lengths.Where(x => x != null).Select(x => x![0]))}}
                      default:
                          return false;
                  }
              }
          """;

    private string GenerateNormal() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{genCfg.DataType}}[]?[] _entries = [
          {{JoinValues(ctx.Lengths.Skip((int)ctx.MinLength).Take((int)(ctx.MaxLength - ctx.MinLength + 1)), RenderMany, ",\n")}}
              ];

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}
                  {{genCfg.DataType}}[]? bucket = _entries[value.Length - {{ctx.MinLength}}];

                  if (bucket == null)
                      return false;

                  foreach ({{genCfg.DataType}} str in bucket)
                  {
                      if ({{genCfg.GetEqualFunction("value", "str")}})
                          return true;
                  }

                  return false;
              }
          """;

    private static string GenerateSwitch(GeneratorConfig genCfg, IEnumerable<string> values)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string value in values)
        {
            sb.AppendLine($"""
                                       case {value.Length}:
                                           return {genCfg.GetEqualFunction("value", ToValueLabel(value))};
                           """);
        }

        return sb.ToString();
    }

    private static string GetEarlyExit(IEarlyExit[] exits)
    {
        //We do this to force an early exit for this data structure. Otherwise, we will get an IndexOutOfRangeException
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit1.MinValue, exit1.MaxValue, true);

        MinMaxValueEarlyExit? exit2 = (MinMaxValueEarlyExit?)Array.Find(exits, x => x is MinMaxValueEarlyExit);
        if (exit2 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit2.MinValue, exit2.MaxValue, false);

        LengthBitSetEarlyExit? exit3 = (LengthBitSetEarlyExit?)Array.Find(exits, x => x is LengthBitSetEarlyExit);
        if (exit3 != null)
            return CSharpGeneratorConfigExtensions.GetMaskEarlyExit("value", exit3.BitSet);

        throw new InvalidOperationException("No early exits were found. They are required for UniqueKeyLength");
    }

    private static void RenderOne(StringBuilder sb, string? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        ").Append(ToValueLabel(obj));
    }

    private static void RenderMany(StringBuilder sb, List<string>? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        [").Append(string.Join(",", obj.Select(ToValueLabel))).Append(']');
    }
}