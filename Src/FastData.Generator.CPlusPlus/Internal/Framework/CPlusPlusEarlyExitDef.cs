using Genbox.FastData.Generator.CPlusPlus.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusEarlyExitDef(TypeHelper helper, CPlusPlusOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CPlusPlusOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}ULL & (1ULL << (value.length() - 1))) == 0)
                     return false;
         """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {helper.ToValueLabel(max)}" : $"value < {helper.ToValueLabel(min)} || value > {helper.ToValueLabel(max)}")})
                     return false;
         """;

    protected override string GetLengthEarlyExits(uint min, uint max) =>
        $"""
                 if ({(min.Equals(max) ? $"value.length() != {helper.ToValueLabel(max)}" : $"const size_t len = value.length(); len < {helper.ToValueLabel(min)} || len > {helper.ToValueLabel(max)}")})
                     return false;
         """;
}