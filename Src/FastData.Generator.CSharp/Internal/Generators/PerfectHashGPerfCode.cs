using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              private{{cfg.GetModifier()}} {{SmallestType(ctx.MaxHash + 1)}}[] _assoValues = new {{SmallestType(ctx.MaxHash + 1)}}[] {
          {{JoinValues(ctx.AssociationValues, RenderAsso)}}
              };

              private{{cfg.GetModifier()}} string[] _wordlist = new string[] {
          {{RenderWords(ctx.Items)}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}
                  uint key = hash(value);
                  return _wordlist[key] == value;
              }

              public uint hash(string str)
              {
                  return (uint)({{JoinValues(ctx.Positions, RenderPositions, "&&")}});
              }
          """;

    private static string RenderWords(KeyValuePair<string, uint>[] items)
    {
        StringBuilder sb = new StringBuilder();

        uint index = 0;
        foreach (KeyValuePair<string, uint> pair in items)
        {
            if (index < pair.Value)
            {
                uint count = pair.Value - index;

                while (count-- != 0)
                    sb.Append("\"\",");

                index = pair.Value;
            }

            sb.Append($"\"{pair.Key}\",");

            index++;
        }
        return sb.ToString();
    }

    private static void RenderAsso(StringBuilder sb, int value) => sb.Append(value.ToString(NumberFormatInfo.InvariantInfo));

    private static void RenderPositions(StringBuilder sb, int position)
    {
        if (position == -1)
            sb.Append("_assoValues[str[str.Length -1]]");
        else
            sb.Append("_assoValues[str[" + position + "]]");
    }

    private static string SmallestType(int max) => max switch
    {
        _ => "int"
    };
}