using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class CSharpGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this CSharpGeneratorConfig cfg, GeneratorConfig genCfg, string variable)
    {
        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableEarlyExits))
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in genCfg.EarlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetValueEarlyExits(variable, minLength, maxLength, true));
            else if (spec is MinMaxValueEarlyExit(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(variable, minValue, maxValue, false));
            else if (spec is MinMaxUnsignedValueEarlyExit(var minUValue, var maxUValue))
                sb.Append(GetValueEarlyExits(variable, minUValue, maxUValue, false));
            else if (spec is MinMaxFloatValueEarlyExit(var minFloatValue, var maxFloatValue))
                sb.Append(GetValueEarlyExits(variable, minFloatValue, maxFloatValue, false));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(variable, bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    internal static string GetModFunction(this CSharpGeneratorConfig config, string variable, uint length)
    {
        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"{variable} % {length.ToString(NumberFormatInfo.InvariantInfo)}";

        if (MathHelper.IsPowerOfTwo(length))
            return $"{variable} & {(length - 1).ToString(NumberFormatInfo.InvariantInfo)}";

        ulong modMultiplier = MathHelper.GetFastModMultiplier(length);
        return $"MathHelper.FastMod({variable}, {length.ToString(NumberFormatInfo.InvariantInfo)}, {modMultiplier.ToString(NumberFormatInfo.InvariantInfo)})";
    }

    internal static string? GetMethodAttributes(this CSharpGeneratorConfig config)
    {
        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
            return "[MethodImpl(MethodImplOptions.NoInlining)]";

        if (config.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
            return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        return null;
    }

    internal static string? GetModifier(this CSharpGeneratorConfig config) => config.ClassType == ClassType.Static ? " static" : null;

    internal static string GetMaskEarlyExit(string variable, ulong bitSet)
        => $"""
                   if (({bitSet}UL & 1UL << ({variable}.Length -1) & 63) == 0)
                       return false;
            """;

    internal static string GetValueEarlyExits<T>(string variable, T min, T max, bool length) where T : IFormattable
        => min.Equals(max)
            ? $"""
                      if ({variable}{(length ? ".Length" : "")} != {max})
                          return false;
               """
            : $"""
                       if ({variable}{(length ? ".Length" : "")} < {min} || {variable}{(length ? ".Length" : "")} > {max})
                          return false;
               """;
}