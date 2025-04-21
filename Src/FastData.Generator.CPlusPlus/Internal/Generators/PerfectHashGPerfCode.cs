namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate()
    {
        string?[] items = WrapWords(ctx.Items).ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}} std::array<{{GetSmallestSignedType(ctx.MaxHash + 1)}}, {{ctx.AssociationValues.Length}}> asso = {
                 {{FormatColumns(ctx.AssociationValues, RenderAssociativeValue)}}
                     };

                     {{cfg.GetFieldModifier()}} std::array<std::string, {{items.Length}}> items = {
                 {{FormatColumns(items, static (sb, x) => sb.Append(ToValueLabel(x)))}}
                     };

                 public:
                     {{cfg.GetMethodModifier()}} bool contains(const {{genCfg.GetTypeName()}}& value)
                     {
                 {{cfg.GetEarlyExits(genCfg)}}

                         const uint32_t hash = get_hash(value);

                         if (hash > {{ctx.MaxHash.ToString(NumberFormatInfo.InvariantInfo)}})
                             return false;

                         return {{genCfg.GetEqualFunction("items[hash]")}};
                     }

                     {{cfg.GetMethodModifier()}} uint32_t get_hash(const std::string& str)
                     {
                 {{RenderHashFunction()}}
                     }

                 """;
    }

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
                                                     uint32_t hash = 0;
                                                     switch (str.length())
                                                     {
                                                         default:

                                             """);

        // Output the default case
        sb.Append("                hash += ");
        RenderAsso(sb, key);
        sb.AppendLine(";");

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
            sb.Append("static_cast<uint32_t>(asso[str[str.length() - 1]])");
        else
        {
            sb.Append("static_cast<uint32_t>(asso[str[").Append(pos).Append(']');

            if (ctx.AlphaIncrements[pos] != 0)
                sb.Append('+').Append(ctx.AlphaIncrements[pos]);

            sb.Append("])");
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