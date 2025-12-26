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

    protected override string GetLengthEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) =>
        $$"""
                  if {{(minByte.Equals(maxByte) ? $"key.len() != {map.ToValueLabel(maxByte)} as usize" : $"key.len() < {map.ToValueLabel(minByte)} as usize || key.len() > {map.ToValueLabel(maxByte)} as usize")}} {
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