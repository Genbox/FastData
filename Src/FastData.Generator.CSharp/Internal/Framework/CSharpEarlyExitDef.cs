using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpEarlyExitDef(TypeMap map, CSharpOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CSharpOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}UL & (1UL << (value.Length - 1))) == 0)
                     return false;
         """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {map.ToValueLabel(max)}" : $"value < {map.ToValueLabel(min)} || value > {map.ToValueLabel(max)}")})
                     return false;
         """;

    protected override string GetLengthEarlyExits(uint min, uint max, uint minByte, uint maxByte) =>
        $"""
                 if ({(min.Equals(max) ? $"value.Length != {map.ToValueLabel(max)}" : $"value.Length < {map.ToValueLabel(min)} || value.Length > {map.ToValueLabel(max)}")})
                     return false;
         """;
}