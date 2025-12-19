using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Rust.Enums;
using System.Text;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustEarlyExitDef(TypeMap map, RustOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(RustOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(MethodType methodType, ulong[] bitSet)
    {
        if (bitSet.Length == 1)
            return RenderWord(bitSet[0], methodType);

        StringBuilder sb = new StringBuilder();

        sb.Append("    match key.len() >> 6 {");

        for (int i = 0; i < bitSet.Length; i++)
        {
            sb.Append($$"""

                                {{i.ToStringInvariant()}} => {
                        {{RenderWord(bitSet[i], methodType)}}
                                },
                        """);
        }

        sb.Append($$"""

                            _ => {
                                 {{RenderMethod(methodType)}}
                             }
                        }
                    """);

        return sb.ToString();
    }

    protected override string GetValueEarlyExits<T>(MethodType methodType, T min, T max) =>
        $$"""
                  if {{(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")}} {
                      {{RenderMethod(methodType)}}
                  }
          """;

    protected override string GetLengthEarlyExits(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) =>
        $$"""
                  if {{(minByte.Equals(maxByte) ? $"key.len() != {map.ToValueLabel(maxByte)} as usize" : $"key.len() < {map.ToValueLabel(minByte)} as usize || key.len() > {map.ToValueLabel(maxByte)} as usize")}} {
                      {{RenderMethod(methodType)}}
                  }
          """;

    private static string RenderWord(ulong word, MethodType methodType) =>
        $$"""
                      if {{word.ToStringInvariant()}}u64 & (1u64 << ((key.len().wrapping_sub(1)) & 63)) == 0 {
                          {{RenderMethod(methodType)}}
                      }
          """;

    private static string RenderMethod(MethodType methodType) => methodType == MethodType.TryLookup ? "return None;" : "return false;";
}