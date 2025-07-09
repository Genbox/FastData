using Genbox.FastData.Generator.CPlusPlus.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusEarlyExitDef(TypeMap map, CPlusPlusOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CPlusPlusOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}ULL & (1ULL << (value.length() - 1))) == 0)
                     return false;
         """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {map.ToValueLabel(max)}" : $"value < {map.ToValueLabel(min)} || value > {map.ToValueLabel(max)}")})
                     return false;
         """;

    protected override string GetLengthEarlyExits(uint min, uint max, uint minByte, uint maxByte) =>
        $"""
                 if ({(min.Equals(max) ? $"value.length() != {map.ToValueLabel(max)}" : $"const size_t len = value.length(); len < {map.ToValueLabel(min)} || len > {map.ToValueLabel(max)}")})
                     return false;
         """;
}