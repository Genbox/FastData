namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}[] _asso = new {{GetSmallestUnsignedType(ctx.MaxHash + 1)}}[] {
          {{FormatColumns(ctx.AssociationValues, RenderAssociativeValue)}}
              };

              {{cfg.GetFieldModifier()}}string?[] _items = {
          {{FormatColumns(WrapWords(ctx.Items), static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  uint hash = Hash(value);

                  if (hash > {{ctx.MaxHash.ToString(NumberFormatInfo.InvariantInfo)}})
                      return false;

                  return {{genCfg.GetEqualFunction("_items[hash]")}};
              }

              {{cfg.GetMethodModifier(true)}}uint Hash(string str)
              {
          {{RenderHashFunction()}}
              }

          """;

    private string RenderHashFunction()
    {
        //IQ: We can assume there always are positions present
        //IQ: We also assume that positions are listed in descending order

        //We need to know the shortest string
        uint minLen = (uint)genCfg.Constants.MinValue;

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
                                                               goto case {{key}};

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

    private static void RenderAssociativeValue(StringBuilder sb, int value) => sb.Append(value.ToString(NumberFormatInfo.InvariantInfo));
}