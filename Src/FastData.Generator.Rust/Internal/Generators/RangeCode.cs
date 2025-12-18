using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class RangeCode<TKey> : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        string? nanGuard = GeneratorConfig.KeyType switch
        {
            KeyType.Single => """
                              if key.is_nan() {
                                  return false;
                              }
                              """,
            KeyType.Double => """
                              if key.is_nan() {
                                  return false;
                              }
                              """,
            _ => null
        };

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    """);

        if (nanGuard != null)
            sb.AppendLine(nanGuard);

        sb.Append("""

                          return key >= Self::MIN_KEY && key <= Self::MAX_KEY;
                      }
                  """);

        return sb.ToString();
    }
}