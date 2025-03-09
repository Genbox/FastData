using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, KeyLengthContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} readonly {{genCfg.DataType}}[]?[] _entries = [
          {{JoinValues(ctx.Lengths, RenderEntry, ",\n")}}
              ];

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{GetEarlyExit(genCfg.EarlyExits)}}
                  {{genCfg.DataType}}[]? bucket = _entries[value.Length];

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

    private static string GetEarlyExit(IEarlyExit[] exits)
    {
        //We do this to force an early exit for this data structure. Otherwise, we will IndexOutOfRangeException
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit1.MinValue, exit1.MaxValue, true);

        MinMaxValueEarlyExit? exit2 = (MinMaxValueEarlyExit?)Array.Find(exits, x => x is MinMaxValueEarlyExit);
        if (exit2 != null)
            return CSharpGeneratorConfigExtensions.GetValueEarlyExits("value", exit2.MinValue, exit2.MaxValue, false);

        throw new InvalidOperationException("No early exits were found. They are required for KeyLength");
    }

    private static void RenderEntry(StringBuilder sb, List<string>? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        [").Append(string.Join(",", obj.Select(x => $"\"{x}\""))).Append(']');
    }
}