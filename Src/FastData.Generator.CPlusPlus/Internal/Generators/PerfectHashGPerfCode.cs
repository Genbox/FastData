using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class PerfectHashGPerfCode<T>(PerfectHashGPerfContext ctx, GeneratorConfig<T> genCfg) : OutputWriter<T>
{
    public override string Generate()
    {
        string?[] items = WrapWords(ctx.Items).ToArray();

        return $$"""
                     {{GetFieldModifier()}}std::array<{{GetSmallestUnsignedType(ctx.MaxHash + 1)}}, {{ctx.AssociationValues.Length}}> asso = {
                 {{FormatColumns(ctx.AssociationValues, static x => x.ToStringInvariant())}}
                     };

                     {{GetFieldModifier()}}std::array<{{GetTypeName()}}, {{items.Length}}> items = {
                 {{FormatColumns(items, ToValueLabel)}}
                     };

                 public:
                     {{GetMethodAttributes()}}
                     {{GetMethodModifier()}}bool contains(const {{GetTypeName()}} value) noexcept
                     {
                 {{GetEarlyExits()}}

                         const uint32_t hash = get_hash(value);

                         if (hash > {{ctx.MaxHash.ToStringInvariant()}})
                             return false;

                         return {{GetEqualFunction("items[hash]", "value")}};
                     }

                     {{GetMethodModifier()}}uint32_t get_hash(const {{GetTypeName()}} str)
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
        uint minLen = genCfg.Constants.MinStringLength;

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