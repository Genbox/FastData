using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class RangeCode<TKey> : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        string? nanGuard = GeneratorConfig.KeyType switch
        {
            KeyType.Single or KeyType.Double => """
                                                if (key != key)
                                                    return false;
                                                """,
            _ => null
        };

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    """);

        if (nanGuard != null)
            sb.AppendLine(nanGuard);

        sb.Append("""

                          return key >= min_key && key <= max_key;
                      }
                  """);

        return sb.ToString();
    }
}