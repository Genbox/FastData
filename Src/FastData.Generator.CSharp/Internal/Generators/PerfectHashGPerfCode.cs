using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} {{GetSmallestSignedType(ctx.MaxHash + 1)}}[] _asso = new {{GetSmallestSignedType(ctx.MaxHash + 1)}}[] {
          {{FormatColumns(ctx.AssociationValues, RenderAsso, 8)}}
              };

              private{{cfg.GetModifier()}} string[] _items = {
          {{FormatColumns(WrapWords(ctx.Items), static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  uint key = Hash(value);

                  if (key > {{ctx.MaxHash.ToString(NumberFormatInfo.InvariantInfo)}})
                      return false;

                  return {{genCfg.GetEqualFunction("_items[key]", "value")}};
              }

              private {{cfg.GetModifier()}} uint Hash(string str) => (uint)({{FormatList(ctx.Positions, RenderPositions, " + ")}});
          """;

    private static IEnumerable<string?> WrapWords(KeyValuePair<string, uint>[] items)
    {
        uint index = 0;
        foreach (KeyValuePair<string, uint> pair in items)
        {
            if (index < pair.Value)
            {
                uint count = pair.Value - index;

                while (count-- != 0)
                    yield return null;

                index = pair.Value;
            }

            yield return pair.Key;
            index++;
        }
    }

    private static void RenderAsso(StringBuilder sb, int value) => sb.Append(value.ToString(NumberFormatInfo.InvariantInfo));

    private static void RenderPositions(StringBuilder sb, int position)
    {
        if (position == -1)
            sb.Append("_asso[str[str.Length - 1]]");
        else
            sb.Append("_asso[str[").Append(position).Append("]]");
    }
}