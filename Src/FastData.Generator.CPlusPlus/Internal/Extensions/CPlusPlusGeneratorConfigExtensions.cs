using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.CPlusPlus.Enums;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class CPlusPlusGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this CPlusPlusCodeGeneratorConfig cfg, GeneratorConfig genCfg)
    {
        if (cfg.GeneratorOptions.HasFlag(CPlusPlusOptions.DisableEarlyExits))
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in genCfg.EarlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetLengthEarlyExits(minLength, maxLength));
            else if (spec is MinMaxValueEarlyExit(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(minValue, maxValue, genCfg.DataType));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    public static string GetFieldModifier(this CPlusPlusCodeGeneratorConfig config, bool constexpr = true) => constexpr ? "static constexpr " : "inline static const ";

    public static string GetMethodModifier(this CPlusPlusCodeGeneratorConfig config) => "static ";

    internal static string GetModFunction(this CPlusPlusCodeGeneratorConfig config, int length)
    {
        return $"hash % {length.ToString(NumberFormatInfo.InvariantInfo)}";
    }

    internal static string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}ULL & 1ULL << (value.length() - 1) % 64) == 0)
                     return false;
         """;

    internal static string GetValueEarlyExits(object min, object max, DataType dataType) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {ToValueLabel(max, dataType)}" : $"value < {ToValueLabel(min, dataType)} || value > {ToValueLabel(max, dataType)}")})
                     return false;
         """;

    internal static string GetLengthEarlyExits(uint min, uint max) =>
        $"""
                 if (const size_t len = value.length(); {(min.Equals(max) ? $"len != {ToValueLabel(max)}" : $"len < {ToValueLabel(min)} || len > {ToValueLabel(max)}")})
                     return false;
         """;
}