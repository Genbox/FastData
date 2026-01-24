using System.Diagnostics;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generator.Rust.Enums;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustEarlyExitDef(TypeMap map, RustOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(RustOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(MethodType methodType, ulong[] bitSet)
    {
        return bitSet.Length == 1
            ? RenderWord(bitSet[0], methodType)
            : $$"""
                    match key.len() >> 6 {
                {{RenderCases()}}
                        _ => {
                             {{RenderExit(methodType)}}
                        }
                    }
                """;

        string RenderCases()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bitSet.Length; i++)
            {
                sb.Append($$"""
                                    {{i.ToStringInvariant()}} => {
                            {{RenderWord(bitSet[i], methodType)}}
                                    },

                            """);
            }

            return sb.ToString();
        }
    }

    protected override string GetValueEarlyExit<T>(MethodType methodType, T min, T max) =>
        $$"""
                  if {{(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")}} {
                      {{RenderExit(methodType)}}
                  }
          """;

    protected override string GetValueBitMaskEarlyExit<T>(MethodType methodType, ulong mask)
    {
        Type unsignedType = TypeHelper.GetUnsignedType(typeof(T));
        string unsignedTypeName = map.GetTypeName(unsignedType);
        object maskValue = TypeHelper.ConvertValueToType(mask, unsignedType);
        string maskLiteral = map.ToValueLabel(maskValue, unsignedType);

        return $$"""
                         if (key as {{unsignedTypeName}} & {{maskLiteral}}) != 0 {
                             {{RenderExit(methodType)}}
                         }
                 """;
    }

    protected override string GetLengthEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte, GeneratorEncoding encoding)
    {
        if (minByte.Equals(maxByte))
        {
            return $$"""
                             if key.len() != {{map.ToValueLabel(maxByte)}} as usize {
                                 {{RenderExit(methodType)}}
                             }
                     """;
        }

        return $$"""
                         let len = key.len();
                         if len < {{map.ToValueLabel(minByte)}} as usize || len > {{map.ToValueLabel(maxByte)}} as usize {
                             {{RenderExit(methodType)}}
                         }
                 """;
    }

    protected override string GetLengthDivisorEarlyExit(MethodType methodType, uint charDivisor, uint byteDivisor)
    {
        Debug.Assert(byteDivisor > 1);

        return $$"""
                         if key.len() % {{map.ToValueLabel(byteDivisor)}} as usize != 0 {
                             {{RenderExit(methodType)}}
                         }
                 """;
    }

    protected override string GetStringBitMaskEarlyExit(MethodType methodType, ulong mask, int byteCount, bool ignoreCase, GeneratorEncoding encoding)
    {
        if (mask == 0 || byteCount <= 0)
            return string.Empty;

        if (encoding != GeneratorEncoding.UTF8 && encoding != GeneratorEncoding.ASCII)
            return string.Empty;

        if (!ignoreCase)
        {
            StringBuilder expr = new StringBuilder();

            for (int i = 0; i < byteCount; i++)
            {
                if (i > 0)
                    expr.Append(" | ");

                expr.Append($"(bytes[{i}] as u64)");
                if (i > 0)
                    expr.Append($" << {i * 8}");
            }

            return $$"""
                             let bytes = key.as_bytes();
                             let first = {{expr}};

                             if (first & {{mask.ToStringInvariant()}}u64) != 0 {
                                 {{RenderExit(methodType)}}
                             }
                     """;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("""
                        let mut first: u64 = 0;
                        let bytes = key.as_bytes();
                  """);

        for (int i = 0; i < byteCount; i++)
        {
            sb.Append($"""
                                       let mut b{i} = bytes[{i}];
                                       b{i} = to_lower_ascii(b{i});
                                       first |= (b{i} as u64) << {i * 8};
                       """);
        }

        sb.Append($$"""

                                    if (first & {{mask.ToStringInvariant()}}u64) != 0 {
                                        {{RenderExit(methodType)}}
                                    }
                    """);
        return sb.ToString();
    }

    protected override string GetCharRangeEarlyExit(MethodType methodType, char firstMin, char firstMax, char lastMin, char lastMax, bool ignoreCase, GeneratorEncoding encoding) =>
        $$"""
                  let bytes = key.as_bytes();
                  let len = bytes.len();
                  {{(ignoreCase ? "let first_char = to_lower_ascii(bytes[0]);" : "let first_char = bytes[0];")}}
                  {{(ignoreCase ? "let last_char = to_lower_ascii(bytes[len - 1]);" : "let last_char = bytes[len - 1];")}}

                  if first_char < {{map.ToValueLabel((byte)firstMin)}}u8 || first_char > {{map.ToValueLabel((byte)firstMax)}}u8 || last_char < {{map.ToValueLabel((byte)lastMin)}}u8 || last_char > {{map.ToValueLabel((byte)lastMax)}}u8 {
                      {{RenderExit(methodType)}}
                  }
          """;

    protected override string GetPrefixSuffixEarlyExit(MethodType methodType, string prefix, string suffix, bool ignoreCase)
    {
        string prefixCheck = ignoreCase ? $"case_insensitive_starts_with(key, {map.ToValueLabel(prefix)})" : $"key.starts_with({map.ToValueLabel(prefix)})";
        string suffixCheck = ignoreCase ? $"case_insensitive_ends_with(key, {map.ToValueLabel(suffix)})" : $"key.ends_with({map.ToValueLabel(suffix)})";

        string condition;
        if (prefix.Length == 0)
            condition = suffixCheck;
        else
            condition = suffix.Length == 0 ? prefixCheck : $"{prefixCheck} && {suffixCheck}";

        return $$"""
                         if !({{condition}}) {
                             {{RenderExit(methodType)}}
                         }
                 """;
    }

    private static string RenderWord(ulong word, MethodType methodType) =>
        $$"""
                      if {{word.ToStringInvariant()}}u64 & (1u64 << ((key.len().wrapping_sub(1)) & 63)) == 0 {
                          {{RenderExit(methodType)}}
                      }
          """;

    private static string RenderExit(MethodType methodType) => methodType == MethodType.TryLookup ? "return None;" : "return false;";
}