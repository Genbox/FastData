using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpEarlyExitDef(TypeMap map, CSharpOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CSharpOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(MethodType methodType, ulong bitSet) => methodType == MethodType.Contains
        ? $"""
                   if (({bitSet}UL & (1UL << (key.Length - 1))) == 0)
                       return false;
           """
        : $$"""
                    if (({{bitSet}}UL & (1UL << (key.Length - 1))) == 0)
                    {
                        value = default;
                        return false;
                    }
            """;

    protected override string GetValueEarlyExits<T>(MethodType methodType, T min, T max) => methodType == MethodType.Contains
        ? $"""
                   if ({(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")})
                       return false;
           """
        : $$"""
                    if ({{(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")}})
                    {
                        value = default;
                        return false;
                    }
            """;

    protected override string GetLengthEarlyExits(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) => methodType == MethodType.Contains
        ? $"""
                   if ({(min.Equals(max) ? $"key.Length != {map.ToValueLabel(max)}" : $"key.Length < {map.ToValueLabel(min)} || key.Length > {map.ToValueLabel(max)}")})
                       return false;
           """
        : $$"""
                    if ({{(min.Equals(max) ? $"key.Length != {map.ToValueLabel(max)}" : $"key.Length < {map.ToValueLabel(min)} || key.Length > {map.ToValueLabel(max)}")}})
                    {
                        value = default;
                        return false;
                    }
            """;
}