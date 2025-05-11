using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpEarlyExitHandler(CodeHelper helper, CSharpOptions options) : EarlyExitHandler
{
    protected override bool IsEnabled => !options.HasFlag(CSharpOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}UL & (1UL << (value.Length - 1) % 64)) == 0)
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