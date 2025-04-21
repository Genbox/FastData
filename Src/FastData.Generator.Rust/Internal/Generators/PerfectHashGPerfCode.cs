namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashGPerfCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, PerfectHashGPerfContext ctx) : IOutputWriter
{
    public string Generate()
    {
        string?[] items = WrapWords(ctx.Items).ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}}const ASSO: [{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}; {{ctx.AssociationValues.Length}}] = [
                 {{FormatColumns(ctx.AssociationValues, RenderAssociativeValue)}}
                     ];

                     {{cfg.GetFieldModifier()}}const ITEMS: [&'static str; {{items.Length}}] = [
                 {{FormatColumns(items, static (sb, x) => sb.Append(ToValueLabel(x)))}}
                     ];

                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}

                         let hash = Self::get_hash(value) as usize;
                         if hash > {{ctx.MaxHash}} {
                             return false;
                         }

                         {{genCfg.GetEqualFunction("Self::ITEMS[hash]")}}
                     }

                     {{cfg.GetMethodModifier()}}fn get_hash(str: &str) -> u32 {
                 {{RenderHashFunction()}}
                     }
                 """;
    }

    private string RenderHashFunction()
    {
        //We need to know the shortest string
        uint minLen = (uint)genCfg.Constants.MinValue;

        //We start with the highest position.
        int key = ctx.Positions[0];

        //If the position is the last char, or is less than the shortest string, we can write a simple expression
        if (key == -1 || key < minLen)
            return RenderExpression();

        StringBuilder sb = new StringBuilder($"""
                                                      let bytes = str.as_bytes();
                                                      let mut hash: u32 = {RenderAsso(key)};

                                              """);

        // Output all the other cases
        do
        {
            sb.Append($"        if bytes.len() > {key} {{");

            if (ctx.Positions.Contains(key - 1))
                sb.AppendLine($" hash += {RenderAsso(key - 1)}; }}");
        } while (--key > minLen);

        sb.Append("        return hash");

        if (key == -1)
            sb.Append($" + {RenderAsso(-1)}");

        sb.Append(';');

        return sb.ToString();
    }

    private string RenderExpression()
    {
        StringBuilder sb = new StringBuilder("        return ");

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
            return "Self::ASSO[bytes[bytes.len() - 1] as usize] as u32";

        int inc = ctx.AlphaIncrements[pos];
        return $"Self::ASSO[bytes[{pos}]{(inc != 0 ? $" + {inc}" : "")} as usize] as u32";
    }

    private static IEnumerable<string?> WrapWords(KeyValuePair<string, uint>[] items)
    {
        uint index = 0;
        foreach (var pair in items)
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