using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpEarlyExitDef(TypeHelper helper, CSharpOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(CSharpOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}UL & (1UL << (value.Length - 1))) == 0)
                     return false;
         """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {helper.ToValueLabel(max)}" : $"value < {helper.ToValueLabel(min)} || value > {helper.ToValueLabel(max)}")})
                     return false;
         """;

    protected override string GetLengthEarlyExits(uint min, uint max) =>
        $"""
                 if ({(min.Equals(max) ? $"value.Length != {helper.ToValueLabel(max)}" : $"value.Length < {helper.ToValueLabel(min)} || value.Length > {helper.ToValueLabel(max)}")})
                     return false;
         """;
}