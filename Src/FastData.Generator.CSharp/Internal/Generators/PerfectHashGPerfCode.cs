using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashGPerfCode<T>(PerfectHashGPerfContext ctx, GeneratorConfig<T> genCfg, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}[] _asso = new {{GetSmallestUnsignedType(ctx.MaxHash + 1)}}[] {
          {{FormatColumns(ctx.AssociationValues, static x => x.ToStringInvariant())}}
              };

              {{GetFieldModifier()}}string[] _items = {
          {{FormatColumns(WrapWords(ctx.Items), ToValueLabel)}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{GetTypeName()}} value)
              {
          {{GetEarlyExits()}}

                  uint hash = Hash(value);

                  if (hash > {{ctx.MaxHash.ToStringInvariant()}})
                      return false;

                  return {{GetEqualFunction("value", "_items[hash]")}};
              }

              private {{(cfg.ClassType == ClassType.Static ? "static " : "")}}uint Hash(string str)
              {
          {{RenderHashFunction()}}
              }

          """;

    private string RenderHashFunction()
    {
        //IQ: We can assume there always are positions present
        //IQ: We also assume that positions are listed in descending order

        //We need to know the shortest string
        uint minLen = genCfg.Constants.MinStringLength;

        //We start with the highest position.
        int key = ctx.Positions[0];

        //If the position is the last char, or is less than the shortest string, we can write a simple expression
        if (key == -1 || key < minLen)
            return RenderExpression();

        StringBuilder sb = new StringBuilder($$"""
                                                       uint hash = 0;
                                                       switch (str.Length)
                                                       {
                                                           default:
                                                               hash += {{RenderAsso(key)}};
                                                               goto case {{key.ToStringInvariant()}};

                                               """);

        // Output all the other cases
        do
        {
            sb.AppendLine($"            case {key}:");

            if (ctx.Positions.Contains(key - 1))
                sb.AppendLine($"                hash += {RenderAsso(key - 1)};");

            if (key != minLen)
                sb.AppendLine($"                goto case {key - 1};");
        } while (--key > minLen);

        if (key == minLen)
            sb.AppendLine($"            case {minLen}:");

        if (ctx.Positions.Contains(key))
            sb.AppendLine($"                hash += {RenderAsso(key - 1)};");

        sb.Append("""
                                  break;
                          }

                          return hash
                  """);

        if (key == -1)
            sb.Append($" + {RenderAsso(-1)}");

        sb.Append(';');

        return sb.ToString();
    }

    private string RenderExpression()
    {
        StringBuilder sb = new StringBuilder("        return ");

        // Render each position. We should get "asso[str[x]] + asso[str[y]] + ..."
        for (int i = 0; i < ctx.Positions.Length; i++)
        {
            sb.Append(RenderAsso(ctx.Positions[i]));

            if (i < ctx.Positions.Length - 1)
                sb.Append(" + ");
        }

        sb.Append(';');
        return sb.ToString();
    }

    private string RenderAsso(int pos)
    {
        if (pos == -1)
            return "(uint)_asso[str[str.Length - 1]]";

        int inc = ctx.AlphaIncrements[pos];
        return $"(uint)_asso[str[{pos}]{(inc != 0 ? $" + {inc}" : "")}]";
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
}