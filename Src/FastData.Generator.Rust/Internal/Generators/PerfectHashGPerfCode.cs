using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashGPerfCode<T>(PerfectHashGPerfContext ctx, GeneratorConfig<T> genCfg) : RustOutputWriter<T>
{
    public override string Generate()
    {
        string?[] items = WrapWords(ctx.Items).ToArray();

        return $$"""
                     {{GetFieldModifier()}}const ASSO: [{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}; {{ctx.AssociationValues.Length}}] = [
                 {{FormatColumns(ctx.AssociationValues, static x => x.ToStringInvariant())}}
                     ];

                     {{GetFieldModifier()}}const ITEMS: [&'static str; {{items.Length}}] = [
                 {{FormatColumns(items, ToValueLabel)}}
                     ];

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                 {{GetEarlyExits()}}

                         let hash = unsafe { Self::get_hash(value) } as usize;
                         if hash > {{ctx.MaxHash}} {
                             return false;
                         }

                         return Self::ITEMS[hash] == value;
                     }

                     fn get_hash(str: &str) -> u32 {
                 {{RenderHashFunction()}}
                     }
                 """;
    }

    private string RenderHashFunction()
    {
        //We need to know the shortest string
        uint minLen = genCfg.Constants.MinStringLength;

        //We start with the highest position.
        int key = ctx.Positions[0];

        //If the position is the last char, or is less than the shortest string, we can write a simple expression
        if (key == -1 || key < minLen)
            return RenderExpression();

        //We add one to key to have one more branch for the usual default case
        key++;

        StringBuilder sb = new StringBuilder("""
                                                     let bytes = str.as_bytes();
                                                     let mut hash: u32 = 0;

                                             """);

        // Output all the other cases
        do
        {
            sb.Append($"        if bytes.len() >= {key} {{");

            if (ctx.Positions.Contains(key - 1))
                sb.AppendLine($" hash += {RenderAsso(key - 1)}; }}");
        } while (key-- > minLen);

        sb.Append("        return hash");

        if (key == -1)
            sb.Append($" + {RenderAsso(-1)}");

        sb.Append(';');

        return sb.ToString();
    }

    private string RenderExpression()
    {
        StringBuilder sb = new StringBuilder("""
                                                     let bytes = str.as_bytes();
                                                     return
                                             """);

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
            return " Self::ASSO[bytes[bytes.len() - 1] as usize] as u32";

        int inc = ctx.AlphaIncrements[pos];
        return $" Self::ASSO[bytes[{pos}] as usize{(inc != 0 ? $" + {inc}" : "")}] as u32";
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
                {
                    yield return null;
                }
                index = pair.Value;
            }
            yield return pair.Key;
            index++;
        }
    }
}