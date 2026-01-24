using System.Diagnostics;
using Genbox.FastData.Generator.CPlusPlus.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusEarlyExitDef(TypeMap map, CPlusPlusOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CPlusPlusOptions.DisableEarlyExits);

    protected override string GetLengthBitmapEarlyExit(MethodType methodType, ulong[] bitSet)
    {
        return bitSet.Length == 1
            ? RenderWord(bitSet[0], methodType)
            : $$"""
                        switch (key.length() >> 6)
                        {
                {{RenderCases()}}
                            default:
                                {{RenderExit(methodType)}}
                        }
                """;

        string RenderCases()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bitSet.Length; i++)
            {
                sb.Append($"""
                                       case {i.ToStringInvariant()}:
                               {RenderWord(bitSet[i], methodType)}
                                       break;

                           """);
            }

            return sb.ToString();
        }
    }

    protected override string GetValueEarlyExit<T>(MethodType methodType, T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")})
                     {RenderExit(methodType)}
         """;

    protected override string GetValueBitMaskEarlyExit<T>(MethodType methodType, ulong mask)
    {
        Type unsignedType = TypeHelper.GetUnsignedType(typeof(T));
        string unsignedTypeName = map.GetTypeName(unsignedType);
        object maskValue = TypeHelper.ConvertValueToType(mask, unsignedType);
        string maskLiteral = map.ToValueLabel(maskValue, unsignedType);

        return $"""
                        if ((static_cast<{unsignedTypeName}>(key) & {maskLiteral}) != 0)
                            {RenderExit(methodType)}
                """;
    }

    protected override string GetLengthEqualEarlyExit(MethodType methodType, uint length, uint byteCount, GeneratorEncoding encoding)
    {
        uint lengthValue = encoding switch
        {
            GeneratorEncoding.ASCII or GeneratorEncoding.UTF8 => byteCount,
            GeneratorEncoding.UTF32 => byteCount / 4,
            GeneratorEncoding.UTF16 => length,
            _ => throw new InvalidOperationException("Unsupported encoding: " + encoding)
        };

        return $"""
                         if (key.length() != {map.ToValueLabel(lengthValue)})
                             {RenderExit(methodType)}
                """;
    }

    protected override string GetLengthRangeEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte, GeneratorEncoding encoding)
    {
        uint minLen;
        uint maxLen;

        switch (encoding)
        {
            case GeneratorEncoding.ASCII:
            case GeneratorEncoding.UTF8:
                minLen = minByte;
                maxLen = maxByte;
                break;
            case GeneratorEncoding.UTF32:
                minLen = minByte / 4;
                maxLen = maxByte / 4;
                break;
            case GeneratorEncoding.UTF16:
                minLen = min;
                maxLen = max;
                break;
            default:
                throw new InvalidOperationException("Unsupported encoding: " + encoding);
        }

        return $"""
                         const size_t len = key.length();
                         if (len < {map.ToValueLabel(minLen)} || len > {map.ToValueLabel(maxLen)})
                             {RenderExit(methodType)}
                """;
    }

    protected override string GetLengthDivisorEarlyExit(MethodType methodType, uint charDivisor, uint byteDivisor)
    {
        Debug.Assert(byteDivisor > 1);

        return $"""
                        if (key.length() % {map.ToValueLabel(byteDivisor)} != 0)
                            {RenderExit(methodType)}
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
            return $"""
                                    uint64_t first = 0;
                                    std::memcpy(&first, key.data(), {byteCount});

                                    if ((first & {mask.ToStringInvariant()}ULL) != 0)
                                        {RenderExit(methodType)}
                    """;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("                uint64_t first = 0;");

        for (int i = 0; i < byteCount; i++)
        {
            sb.Append($"""
                                       uint32_t c{i} = static_cast<uint32_t>(key[{i}]);
                                       c{i} = to_lower_ascii(c{i});
                                       first |= static_cast<uint64_t>(c{i}) << {i * 8};
                       """);
        }

        sb.Append($"""

                                   if ((first & {mask.ToStringInvariant()}ULL) != 0)
                                       {RenderExit(methodType)}
                   """);
        return sb.ToString();
    }

    protected override string GetCharRangeEarlyExit(MethodType methodType, CharPosition position, char min, char max, bool ignoreCase, GeneratorEncoding encoding) =>
        $"""
                 uint32_t valueChar = {(position == CharPosition.First ?
                     ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.front()))" : "static_cast<uint32_t>(key.front())" :
                     ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.back()))" : "static_cast<uint32_t>(key.back())")};
                 if (valueChar < {map.ToValueLabel((uint)min)} || valueChar > {map.ToValueLabel((uint)max)})
                     {RenderExit(methodType)}
         """;

    protected override string GetCharEqualsEarlyExit(MethodType methodType, CharPosition position, char value, bool ignoreCase, GeneratorEncoding encoding) =>
        $"""
                 uint32_t valueChar = {(position == CharPosition.First ?
                     ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.front()))" : "static_cast<uint32_t>(key.front())" :
                     ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.back()))" : "static_cast<uint32_t>(key.back())")};
                 if (valueChar != {map.ToValueLabel((uint)value)})
                     {RenderExit(methodType)}
         """;

    protected override string GetCharBitmapEarlyExit(MethodType methodType, CharPosition position, ulong low, ulong high, bool ignoreCase, GeneratorEncoding encoding) =>
        $$"""
                  uint32_t valueChar = {{(position == CharPosition.First ?
                      ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.front()))" : "static_cast<uint32_t>(key.front())" :
                      ignoreCase ? "to_lower_ascii(static_cast<uint32_t>(key.back()))" : "static_cast<uint32_t>(key.back())")}};
                  if (valueChar > 0x7F)
                      {{RenderExit(methodType)}}
                  if (valueChar < 64)
                  {
                      if (((1ULL << valueChar) & {{low.ToStringInvariant()}}ULL) == 0)
                          {{RenderExit(methodType)}}
                  }
                  else
                  {
                      if (((1ULL << (valueChar - 64)) & {{high.ToStringInvariant()}}ULL) == 0)
                          {{RenderExit(methodType)}}
                  }
          """;

    protected override string GetStringPrefixSuffixEarlyExit(MethodType methodType, string prefix, string suffix, bool ignoreCase)
    {
        string prefixCheck = ignoreCase ? $"case_insensitive_starts_with(key, {map.ToValueLabel(prefix)})" : $"key.compare(0, {prefix.Length.ToStringInvariant()}, {map.ToValueLabel(prefix)}) == 0";
        string suffixCheck = ignoreCase ? $"case_insensitive_ends_with(key, {map.ToValueLabel(suffix)})" : $"key.compare(key.length() - {suffix.Length.ToStringInvariant()}, {suffix.Length.ToStringInvariant()}, {map.ToValueLabel(suffix)}) == 0";

        string condition;
        if (prefix.Length == 0)
            condition = suffixCheck;
        else
            condition = suffix.Length == 0 ? prefixCheck : $"{prefixCheck} && {suffixCheck}";

        return $"""
                        if (!({condition}))
                            {RenderExit(methodType)}
                """;
    }

    private static string RenderWord(ulong word, MethodType methodType) =>
        $"""
                 if (({word.ToStringInvariant()}ULL & (1ULL << ((key.length() - 1) & 63))) == 0)
                     {RenderExit(methodType)}
         """;

    private static string RenderExit(MethodType methodType) => methodType == MethodType.TryLookup
        ? """
          {
              value = nullptr;
              return false;
          }
          """
        : "return false;";
}