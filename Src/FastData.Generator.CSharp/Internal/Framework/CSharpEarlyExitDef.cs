using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Helpers;
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

    protected override string GetLengthEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) =>
        $"""
                 if ({(min.Equals(max) ? $"key.Length != {map.ToValueLabel(max)}" : $"key.Length < {map.ToValueLabel(min)} || key.Length > {map.ToValueLabel(max)}")})
                     {RenderExit(methodType)}
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