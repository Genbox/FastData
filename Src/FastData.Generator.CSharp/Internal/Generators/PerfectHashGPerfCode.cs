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
          {{FormatColumns(ctx.AssociationValues, RenderAssociativeValue)}}
              };

              private{{cfg.GetModifier()}} string[] _items = {
          {{FormatColumns(WrapWords(ctx.Items), static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  uint hash = Hash(value);

                  if (hash > {{ctx.MaxHash.ToString(NumberFormatInfo.InvariantInfo)}})
                      return false;

                  return {{genCfg.GetEqualFunction("_items[hash]")}};
              }

              private{{cfg.GetModifier()}} uint Hash(string str)
              {
          {{RenderHashFunction()}}
              }

          """;

    private string RenderHashFunction()
    {
        //IQ: We can assume there always are positions present
        //IQ: We also assume that positions are listed in descending order

        //We need to know the shortest string
        int minLen = ctx.Items.Min(x => x.Key.Length); //TODO: Make this kind of data available to generators again? or maybe special valus in context?

        //We start with the highest position.
        int key = ctx.Positions[0];

        //If the position is the last char, or is less than the shortest string, we can write a simple expression
        if (key == -1 || key < minLen)
            return RenderExpression();

        StringBuilder sb = new StringBuilder("""
                                                     uint hash = 0;
                                                     switch (str.Length)
                                                     {
                                                         default:

                                             """);

        // Output the default case
        sb.Append("                hash += ");
        RenderAsso(sb, key);
        sb.AppendLine(";");
        sb.AppendLine($"                goto case {key};");

        // Output all the other cases
        do
        {
            sb.AppendLine($"            case {key}:");

            if (ctx.Positions.Contains(key - 1))
            {
                sb.Append("                hash += ");
                RenderAsso(sb, key - 1);
                sb.AppendLine(";");
            }

            if (key != minLen)
                sb.AppendLine($"                goto case {key - 1};");
        } while (--key > minLen);

        if (key == minLen)
            sb.Append("            case ").Append(minLen).AppendLine(":");

        sb.Append("""
                                  break;
                              }

                              return hash
                  """);

        if (key == -1)
        {
            sb.Append(" + ");
            RenderAsso(sb, -1);
        }

        sb.Append(';');

        return sb.ToString();
    }

    private string RenderExpression()
    {
        StringBuilder sb = new StringBuilder("        return ");

        // Render each position. We should get "asso[str[x]] + asso[str[y]] + ..."
        for (int i = 0; i < ctx.Positions.Length; i++)
        {
            RenderAsso(sb, ctx.Positions[i]);

            if (i < ctx.Positions.Length - 1)
                sb.Append(" + ");
        }

        sb.Append(';');
        return sb.ToString();
    }

    private void RenderAsso(StringBuilder sb, int pos)
    {
        if (pos == -1)
            sb.Append("(uint)_asso[str[str.Length - 1]]");
        else
        {
            sb.Append("(uint)_asso[str[").Append(pos).Append(']');

            if (ctx.AlphaIncrements[pos] != 0)
                sb.Append('+').Append(ctx.AlphaIncrements[pos]);

            sb.Append(']');
        }
    }

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

    private static void RenderAssociativeValue(StringBuilder sb, int value) => sb.Append(value.ToString(NumberFormatInfo.InvariantInfo));
}