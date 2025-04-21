namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate()
    {
        string?[] items = WrapWords(ctx.Items).ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}}std::array<{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}, {{ctx.AssociationValues.Length}}> asso = {
                 {{FormatColumns(ctx.AssociationValues, RenderAssociativeValue)}}
                     };

                     {{cfg.GetFieldModifier()}}std::array<std::string, {{items.Length}}> items = {
                 {{FormatColumns(items, static (sb, x) => sb.Append(ToValueLabel(x)))}}
                     };

                 public:
                     {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value)
                     {
                 {{cfg.GetEarlyExits(genCfg)}}

                         const uint32_t hash = get_hash(value);

                         if (hash > {{ctx.MaxHash.ToString(NumberFormatInfo.InvariantInfo)}})
                             return false;

                         return items[hash] == value;
                     }

                     {{cfg.GetMethodModifier()}}uint32_t get_hash(const std::string& str)
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
        uint minLen = (uint)genCfg.Constants.MinValue;

        //We start with the highest position.
        int key = ctx.Positions[0];

        //If the position is the last char, or is less than the shortest string, we can write a simple expression
        if (key == -1 || key < minLen)
            return RenderExpression();

        StringBuilder sb = new StringBuilder($$"""
                                                       uint32_t hash = 0;
                                                       switch (str.length())
                                                       {
                                                           default:
                                                               hash += {{RenderAsso(key)}};
                                               """);

        // Output all the other cases
        do
        {
            sb.AppendLine($"            case {key}:");

            if (ctx.Positions.Contains(key - 1))
                sb.AppendLine($"                hash += {RenderAsso(key - 1)};");
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
            return "static_cast<uint32_t>(asso[str[str.Length - 1]])";

        int inc = ctx.AlphaIncrements[pos];
        return $"static_cast<uint32_t>(asso[str[{pos}]{(inc != 0 ? $" + {inc}" : "")}])";
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