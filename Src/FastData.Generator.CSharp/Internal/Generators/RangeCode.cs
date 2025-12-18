using Genbox.FastData.Generator.CSharp.Internal.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class RangeCode<TKey>(CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        string? nanGuard = GeneratorConfig.KeyType switch
        {
            KeyType.Single => """
                              if (float.IsNaN(key))
                                  return false;
                              """,
            KeyType.Double => """
                              if (double.IsNaN(key))
                                  return false;
                              """,
            _ => null
        };

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    """);

        if (nanGuard != null)
            sb.AppendLine(nanGuard);

        sb.Append("""

                          return key >= MinKey && key <= MaxKey;
                      }
                  """);

        return sb.ToString();
    }
}