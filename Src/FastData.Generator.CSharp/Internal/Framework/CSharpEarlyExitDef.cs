using System.Diagnostics;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators.EarlyExits;
using static Genbox.FastData.Generator.CSharp.Internal.StringHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpEarlyExitDef(TypeMap map, CSharpOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CSharpOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(MethodType methodType, ulong[] bitSet)
    {
        return bitSet.Length == 1
            ? RenderWord(bitSet[0], methodType)
            : $$"""
                        switch (key.Length >> 6)
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
                        if ((({unsignedTypeName})key & {maskLiteral}) != 0)
                            {RenderExit(methodType)}
                """;
    }

    protected override string GetLengthEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte, GeneratorEncoding encoding)
    {
        if (min.Equals(max))
        {
            return $"""
                            if (key.Length != {map.ToValueLabel(max)})
                                {RenderExit(methodType)}
                    """;
        }

        return $"""
                        int len = key.Length;
                        if (len < {map.ToValueLabel(min)} || len > {map.ToValueLabel(max)})
                            {RenderExit(methodType)}
                """;
    }

    protected override string GetLengthDivisorEarlyExit(MethodType methodType, uint charDivisor, uint byteDivisor)
    {
        Debug.Assert(charDivisor > 1);

        return $"""
                        if ((key.Length % {map.ToValueLabel(charDivisor)}) != 0)
                            {RenderExit(methodType)}
                """;
    }

    protected override string GetStringBitMaskEarlyExit(MethodType methodType, ulong mask, int byteCount, bool ignoreCase, GeneratorEncoding encoding)
    {
        if (mask == 0 || byteCount <= 0 || encoding != GeneratorEncoding.UTF16)
            return string.Empty;

        int charCount = byteCount / 2;
        if (charCount == 0)
            return string.Empty;

        if (!ignoreCase)
        {
            string firstExpr = charCount switch
            {
                1 => "(ulong)key[0]",
                2 => "(ulong)key[0] | ((ulong)key[1] << 16)",
                3 => "(ulong)key[0] | ((ulong)key[1] << 16) | ((ulong)key[2] << 32)",
                4 => "(ulong)key[0] | ((ulong)key[1] << 16) | ((ulong)key[2] << 32) | ((ulong)key[3] << 48)",
                _ => string.Empty
            };

            if (firstExpr.Length == 0)
                return string.Empty;

            return $"""
                            ulong first = {firstExpr};

                            if ((first & {mask.ToStringInvariant()}UL) != 0)
                                {RenderExit(methodType)}
                    """;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("                ulong first = 0;");

        for (int i = 0; i < charCount; i++)
        {
            string varName = $"c{i}";
            sb.Append($"""
                                       uint {varName} = key[{i}];
                                       if ({varName} - 'A' <= 'Z' - 'A') {varName} |= 0x20u;
                       """);

            if (i == 0)
                sb.Append($"                first |= {varName};");
            else
                sb.Append($"                first |= (ulong){varName} << {i * 16};");
        }

        sb.Append($"""

                                   if ((first & {mask.ToStringInvariant()}UL) != 0)
                                       {RenderExit(methodType)}
                   """);
        return sb.ToString();
    }

    protected override string GetCharRangeEarlyExit(MethodType methodType, CharPosition position, char min, char max, bool ignoreCase, GeneratorEncoding encoding) =>
        $"""
                 char valueChar = {(position == CharPosition.First ?
                     ignoreCase ? "(char)(key[0] | 0x20)" : "key[0]" :
                     ignoreCase ? "(char)(key[key.Length - 1] | 0x20)" : "key[key.Length - 1]")};
                 if (valueChar < {map.ToValueLabel(min)} || valueChar > {map.ToValueLabel(max)})
                     {RenderExit(methodType)}
         """;

    protected override string GetCharEqualsEarlyExit(MethodType methodType, CharPosition position, char value, bool ignoreCase, GeneratorEncoding encoding) =>
        $"""
                 char valueChar = {(position == CharPosition.First ?
                     ignoreCase ? "(char)(key[0] | 0x20)" : "key[0]" :
                     ignoreCase ? "(char)(key[key.Length - 1] | 0x20)" : "key[key.Length - 1]")};
                 if (valueChar != {map.ToValueLabel(value)})
                     {RenderExit(methodType)}
         """;

    protected override string GetCharBitmapEarlyExit(MethodType methodType, CharPosition position, ulong low, ulong high, bool ignoreCase, GeneratorEncoding encoding) =>
        $$"""
                  uint valueChar = {{(position == CharPosition.First ?
                      ignoreCase ? "(uint)(key[0] | 0x20)" : "key[0]" :
                      ignoreCase ? "(uint)(key[key.Length - 1] | 0x20)" : "key[key.Length - 1]")}};
                  if (valueChar > 0x7F)
                      {{RenderExit(methodType)}}
                  if (valueChar < 64)
                  {
                      if (((1UL << (int)valueChar) & {{low.ToStringInvariant()}}UL) == 0)
                          {{RenderExit(methodType)}}
                  }
                  else
                  {
                      if (((1UL << (int)(valueChar - 64)) & {{high.ToStringInvariant()}}UL) == 0)
                          {{RenderExit(methodType)}}
                  }
          """;

    protected override string GetPrefixSuffixEarlyExit(MethodType methodType, string prefix, string suffix, bool ignoreCase)
    {
        string comparer = GetStringComparer(ignoreCase);
        string prefixCheck = $"key.StartsWith({map.ToValueLabel(prefix)}, StringComparison.{comparer})";
        string suffixCheck = $"key.EndsWith({map.ToValueLabel(suffix)}, StringComparison.{comparer})";

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
                         if (({word.ToStringInvariant()}UL & (1UL << ((key.Length - 1) & 63))) == 0)
                             {RenderExit(methodType)}
         """;

    private static string RenderExit(MethodType methodType) => methodType == MethodType.TryLookup
        ? """
          {
                      value = default;
                      return false;
                  }
          """
        : "return false;";
}