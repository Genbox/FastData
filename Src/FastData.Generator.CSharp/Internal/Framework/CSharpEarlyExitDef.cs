using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

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
                                {{RenderMethod(methodType)}}
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

    protected override string GetValueEarlyExits<T>(MethodType methodType, T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")})
                     {RenderMethod(methodType)}
         """;

    protected override string GetLengthEarlyExits(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) =>
        $"""
                 if ({(min.Equals(max) ? $"key.Length != {map.ToValueLabel(max)}" : $"key.Length < {map.ToValueLabel(min)} || key.Length > {map.ToValueLabel(max)}")})
                     {RenderMethod(methodType)}
         """;

    private static string RenderWord(ulong word, MethodType methodType) =>
        $"""
                         if (({word.ToStringInvariant()}UL & (1UL << ((key.Length - 1) & 63))) == 0)
                             {RenderMethod(methodType)}
         """;

    private static string RenderMethod(MethodType methodType) => methodType == MethodType.TryLookup
        ? """
          {
                      value = default;
                      return false;
                  }
          """
        : "return false;";
}