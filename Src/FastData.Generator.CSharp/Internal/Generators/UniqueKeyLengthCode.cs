using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class UniqueKeyLengthCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, UniqueKeyLengthContext ctx) : IOutputWriter
{
    //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{genCfg.DataType}}[] _entries = new {{genCfg.DataType}}[] {
          {{JoinValues(ctx.Lengths.AsSpan(ctx.LowerBound), Render, ",\n")}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}

                  return {{genCfg.GetEqualFunction("value", $"_entries[value.Length - {ctx.LowerBound.ToString(NumberFormatInfo.InvariantInfo)}]")}};
              }
          """;

    private static string GetEarlyExit(IEarlyExit[] exits)
    {
        //We do this to force an early exit for this data structure. Otherwise, we will IndexOutOfRangeException
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit1.MinValue, exit1.MaxValue, true);

        MinMaxValueEarlyExit? exit2 = (MinMaxValueEarlyExit?)Array.Find(exits, x => x is MinMaxValueEarlyExit);
        if (exit2 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit2.MinValue, exit2.MaxValue, false);

        throw new InvalidOperationException("No early exits were found. They are required for UniqueKeyLength");
    }

    private static void Render(StringBuilder sb, string? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        ").Append(ToValueLabel(obj));
    }
}